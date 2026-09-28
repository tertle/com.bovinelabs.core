# Sparse uploader

`BovineLabs.Core.Graphics.BovineSparseUploader` stages CPU data and dispatches compute-shader copies into a caller-owned raw `GraphicsBuffer`. Its `ThreadedBovineSparseUploader` writer supports Burst jobs and concurrent producers, and `ComputeStats` returns `BovineSparseUploaderStats`. Reference `BovineLabs.Core` from the consuming assembly.

This is an opt-in alternative to `Unity.Rendering.SparseUploader`. Installing Core does not replace Unity's uploader or migrate existing callers.

The Bovine-prefixed type names allow both graphics namespaces to be imported without ambiguous uploader, writer or statistics references. Code using the earlier Core names must adopt these names; no aliases preserve the conflicting names.

## When it helps

- **Many small, equal-sized uploads:** `AddUploads` reserves up to 64 operations with one atomic marker update, reducing per-item atomics and producer contention. Callers must provide batches to obtain this benefit; repeated `AddUpload` calls retain the scalar path.
- **Padded source layouts:** `AddStridedUpload` copies only each element's bytes into compact staging, avoiding copies of unused source padding.
- **A batch fitting one page:** `Begin` maps one page instead of applying the multi-page fragmentation estimate. This avoids large overestimates when one payload nearly fills a page.
- **Generated or gathered upload data:** `TryReserveUpload` lets a producer fill staging directly, avoiding an intermediate allocation and CPU copy. Quill uses this for larger vertex streams.

The bulk path still copies every payload and emits one descriptor per operation. It does not merge GPU operations, deduplicate shared source data or introduce a faster shader. Ordinary large copies and contiguous striding have no established performance advantage. Retaining the upstream shader also means owning a fork whose future Unity fixes must be reviewed and ported explicitly.

## Frame ownership

1. Create the uploader around a valid `GraphicsBuffer` with `GraphicsBuffer.Target.Raw`. The default staging page is 16 MiB; a custom page size must be positive and divisible by four. Keep ownership of the destination buffer; disposing the uploader disposes its staging resources, not that destination.
2. Once per rendered frame, call `Begin(maxDataSizeInBytes, biggestDataUpload, maxOperationCount)` on the owner and pass the returned writer to producers.
3. Finish every producer before calling `EndAndCommit(writer)` on the owner. Source data is copied during each `Add*` call and must remain valid until that call completes. Destination use must follow the submitted uploads in GPU execution order.
4. Call `FrameCleanup()` at the end of the frame. Never retain a writer across commit, cleanup, another Begin or uploader disposal.

Only the threaded writer's producer methods support concurrent use. There is no safety handle or robust stale/wrong-writer validation; callers enforce ownership and synchronization. Use non-overlapping destination writes when order matters.

By default, staging retirement retains the upstream frame-count mechanism. Multiple Begin/commit cycles on one uploader in the same rendered frame are unsupported in this mode: commit count can advance reuse before GPU completion.

Pass `useGraphicsFence: true` to the constructor when a caller can submit multiple batches per rendered frame, including repeated Editor camera renders. This mode requires `SystemInfo.supportsGraphicsFence` and retires each batch only after a CPU-queryable fence following its compute dispatches has passed. It polls without waiting for the GPU, but adds fence creation/polling overhead. Separate batches still require complete Begin/write/commit ownership; concurrent owners are unsupported.

## Budgets and data constraints

`Begin` receives upper bounds for **staged payload bytes**, the largest **individual** staged payload, and descriptor count. Each operation also consumes a 32-byte descriptor, which Begin includes separately. Repeats expand destination writes but do not multiply staged payload bytes or descriptor count.

- For scalar and bulk copies, budget every payload separately, including copies that reuse the same source pointer.
- For a strided operation, total/largest staged payload can be `elementSize * count`; it emits one descriptor. The source needs only `(count - 1) * sourceStride + elementSize` readable bytes for a nonempty call.
- Matrix uploads stage 48 bytes per matrix, including when the source uses 4x4 matrices. Select the matching source/destination matrix formats.
- An individual operation is not split across pages. Its payload plus descriptor must fit a page. Exact page fill is supported when the whole batch fits that page; for multi-page batches, the largest payload plus descriptor must be strictly smaller than the page so the inherited fragmentation denominator remains positive.
- Payload sizes and destination offsets/strides must obey the shader's four-byte addressing. Negative destination stride is supported when every resulting address is valid. Keep byte counts, offsets, budgets and intermediate arithmetic within their signed-int limits; Begin does not provide checked budgeting or range validation.

Insufficient staging capacity logs and returns from the failing `Add*` call. The void API supplies no success result, rollback or batch failure latch: earlier operations can still commit. For bulk calls, a prefix can already have been staged. Calculate sufficient bounds before submitting. Direct Unity allocation-failure logging is retained so the global logger's default filtering does not hide this only failure signal.

## Bulk API

```csharp
void AddUploads(void* src, int size, int sourceStride, int* offsets, int offsetStride,
    int count, int repeatCount = 1, int destinationBias = 0);
```

| Argument | Meaning |
|---|---|
| `src`, `size` | First source payload and positive byte size of every payload; size is a multiple of four. |
| `sourceStride` | Nonnegative byte step between source payloads. Zero reuses the same source for each operation. |
| `offsets`, `offsetStride` | Destination byte offsets and positive step through that array, measured in **integers**. |
| `count` | Number of independent operations. Zero reads neither pointer. |
| `repeatCount` | Contiguous copies of each payload at its destination. Values below one are normalized to one, as in AddUpload. |
| `destinationBias` | Byte offset added to each destination offset; final offsets must be valid, nonnegative, four-byte aligned ints. |

The caller must keep all source/offset ranges valid and reserve sufficient destination space for repeats. Each reservation consumes the available portion of a page before advancing, preserving Begin's single-operation fragmentation bound. Bulk and scalar producers can share a writer.

## Direct writing

```csharp
bool TryReserveUpload(int size, int offsetInBytes, out void* payload, int repeatCount = 1);
```

Reserve an ordinary copy operation and write the final payload directly into the returned staging range. This removes an intermediate allocation and copy when producing disposable upload data, or when gathering several source spans into one upload. It does not remove the GPU scatter operation or the copy required for data that already lives elsewhere.

Size must be positive and divisible by four; the destination must be valid and four-byte aligned. Only four-byte payload alignment is guaranteed. Treat the memory as **write-only**: compute intermediate values in local storage, then write sequentially where possible. Fully initialize every successful reservation before commit. A reservation cannot be cancelled and its pointer expires at commit/cleanup/disposal. Do not wrap it as an independently owned allocation or dispose it.

The method returns false and a null pointer if no page can fit the operation. Unlike the void copy methods, it does not log; the caller handles failure. Budget payload bytes and one descriptor exactly as for `AddUpload`. Direct reservations, scalar copies and bulk copies can share the same writer.

For streams spanning several pages, keep individual payloads comfortably smaller than a page. The multi-page fragmentation estimate divides by the space remaining after the largest payload and its descriptor: near-page-sized operations can produce a very large conservative allocation. Quill uses 1 MiB payloads with 8 MiB staging pages.

## Memory and buffer replacement

Staging pages are pooled and may remain allocated after a workload shrinks. `PruneUploadBufferPoolOnFrameCleanup` requests release of free pages during cleanup; it cannot release pages still retained for submitted work. `ComputeStats` reports allocated internal GPU staging-buffer capacity and Begin's requested upload budget/high-water mark, not measured PCIe transfer bytes.

`ReplaceBuffer(newBuffer, copyFromPrevious: true)` copies the old buffer's full byte size. The caller must supply a sufficiently large new destination and manage the old destination's lifetime.

## Measured results and limits

Editor measurements on 2026-09-28 used Unity 6000.7.0b2, Burst, D3D12, RTX 4070 and Ryzen 9 9950X3D with 31 workers. Three paired runs alternated Stock/Core order, with two repetitions each, ten warmup frames and sixty measured frames per repetition. The faithful initial Core copy had previously demonstrated parity with the installed Unity implementation.

| Synthetic workload | Unity median ms | Core median ms | Reduction |
|---|---:|---:|---:|
| 16,384 small uploads, four producers | 1.10000 | 0.08270 | 92.5% |
| 65,536 small uploads, four producers | 4.14045 | 0.17990 | 95.7% |
| 4 MiB in 1,024 blocks | 0.27830 | 0.18660 | 33.0% |
| 1,031 distinct copies crossing pages, four producers, three repeats | 0.13310 | 0.06070 | 54.4% |
| 65,536 elements, 12 bytes at source stride 64 | 0.20790 | 0.14660 | 29.5% |
| 65,536 elements, 16 bytes at source stride 128 | 0.38590 | 0.29215 | 24.3% |

These measure producer-to-submit CPU time: Begin, producer work and wait, and commit. Cleanup, destination setup and readback are excluded. The bulk comparison uses Core's new batched API against repeated Unity AddUpload calls. Equivalent operation/payload/descriptor/dispatch counts and an independent whole-buffer oracle guard against skipped work; these are workload comparisons, not an isolated timing of the atomic instruction.

A 15 MiB single upload mapped 16 MiB instead of 256 MiB and retained 0 instead of 240 MiB of free staging pages. Its total CPU difference was within observed noise. Small controls remain uncertain: a longer five-run comparison measured a 2 microsecond copy overhead and an 8 microsecond contiguous-stride overhead, both below the larger within-implementation repeat noise floors (4.87 and 11.91 microseconds). Do not assume an across-the-board speedup.

Validation passed 13 focused Editor tests and 41 supported GPU-buffer correctness checks, plus the correctness checks attached to the repeated timing runs. Minimal-source strides and problematic page bounds were excluded only for Stock; unsafe same-frame retirement was excluded for both. These original Editor measurements predate direct reservations, optional fence retirement and Quill adoption. Profile the actual caller before adoption.

Quill's later [upload measurements](../../com.bovinelabs.quill/Documentation~/configuration-and-rendering.md#upload-measurements) compare its actual original backend with direct staging for larger streams and `SetData` for smaller streams. Those improvements include removing Quill's temporary gathering pass; they are not isolated uploader timings.

### Player validation

On the same hardware, a Windows x64 development Player using CoreCLR Checked and Burst confirmed the following producer-to-submit CPU results.
Both implementations ran in the same binary, with three alternating paired runs, two independent repetitions per implementation, ten warmup frames,
and sixty measured frames. Values are medians of the measured samples. GPU timing was disabled; readbacks checked output outside the CPU interval.

| Workload | Unity ms | Core ms | CPU reduction |
|---|---:|---:|---:|
| 16,384 small uploads, four producers | 1.02945 | 0.05695 | 94.5% |
| 65,536 small uploads, four producers | 3.98080 | 0.15350 | 96.1% |
| 1,031 distinct page-crossing copies, three repeats | 0.09620 | 0.02810 | 70.8% |
| 4 MiB in 1,024 blocks | 0.24970 | 0.15500 | 37.9% |
| 65,536 elements, 16 bytes at source stride 128 | 0.31955 | 0.26950 | 15.7% |
| 65,536 elements, 12 bytes at source stride 64 | 0.16125 | 0.17300 | -7.3%, within repeat noise |
| One 4 KiB copy | 0.01560 | 0.01620 | Within repeat noise |
| One 4 MiB copy | 0.17745 | 0.17850 | Within repeat noise |
| One 15 MiB copy | 0.67925 | 0.68065 | Within repeat noise |

The five positive gains exceeded both the larger observed A/A noise floor and 5% of Unity's median. The 12-byte strided case did not reproduce its
Editor gain: its 11.75 microsecond slowdown was below the 14.16 microsecond repeat-noise floor. The 15 MiB copy still mapped 16 MiB rather than
256 MiB and retained 0 rather than 240 MiB of free staging capacity, despite unchanged CPU timing.

The Player passed 41 standalone correctness cases, with five unsupported implementation/case combinations explicitly excluded, plus the readbacks
attached to all benchmark repetitions. Separate direct-reservation checks verified Burst execution, four concurrent producer partitions, exact-page
exhaustion, repeat normalization, and 48 fence-protected commits in one frame. Other graphics APIs, release configurations and whole-game gains remain unverified.

A later focused GPU comparison checked the compacted strided path: 65,536 elements of 16 bytes at source stride 128 measured 2.8416 ms for Unity
and 2.8590 ms for Core, including the same dependent full-buffer GPU copy. The 0.6% difference was small relative to the per-frame variation; this
does not establish a GPU benefit for compaction. The comparison used three alternating pairs, at least 30 warmup frames and 1.5 seconds per variant,
and 60 measured submissions in the same D3D12 development Player, with direct graphics submission for profiling. All six runs verified complete output.
These are inclusive GPU timeline measurements, not isolated shader duration. Quill changes upload methods and GPU operation sizes, and its separate
[GPU comparison](../../com.bovinelabs.quill/Documentation~/configuration-and-rendering.md#upload-measurements) found substantial differences.

## Origin

The initial implementation copies the installed Unity Entities Graphics source at fingerprint `b573f41573f19d7e3aba1683430bbed67d333124`, with the Core namespace/resource path and public control-block allocation APIs. The compute shader is unchanged. The changes described above affect CPU staging and allocation. Unity's copyright and license remain in [Third Party Notices](../Third%20Party%20Notices.md).
