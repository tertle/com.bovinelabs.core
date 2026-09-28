# Sparse uploader

`BovineLabs.Core.Graphics.SparseUploader` stages CPU data and dispatches compute-shader copies into a caller-owned raw `GraphicsBuffer`. Its `ThreadedSparseUploader` writer supports Burst jobs and concurrent producers. Reference `BovineLabs.Core` from the consuming assembly.

This is an opt-in alternative to `Unity.Rendering.SparseUploader`. Installing Core does not replace Unity's uploader or migrate existing callers.

## When it helps

- **Many small, equal-sized uploads:** `AddUploads` reserves up to 64 operations with one atomic marker update, reducing per-item atomics and producer contention. Callers must provide batches to obtain this benefit; repeated `AddUpload` calls retain the scalar path.
- **Padded source layouts:** `AddStridedUpload` copies only each element's bytes into compact staging, avoiding copies of unused source padding.
- **A batch fitting one page:** `Begin` maps one page instead of applying the multi-page fragmentation estimate. This avoids large overestimates when one payload nearly fills a page.

The bulk path still copies every payload and emits one descriptor per operation. It does not merge GPU operations, deduplicate shared source data or introduce a faster shader. Ordinary large copies and contiguous striding have no established performance advantage. Retaining the upstream shader also means owning a fork whose future Unity fixes must be reviewed and ported explicitly.

## Frame ownership

1. Create the uploader around a valid `GraphicsBuffer` with `GraphicsBuffer.Target.Raw`. The default staging page is 16 MiB; a custom page size must be positive and divisible by four. Keep ownership of the destination buffer; disposing the uploader disposes its staging resources, not that destination.
2. Once per rendered frame, call `Begin(maxDataSizeInBytes, biggestDataUpload, maxOperationCount)` on the owner and pass the returned writer to producers.
3. Finish every producer before calling `EndAndCommit(writer)` on the owner. Source data is copied during each `Add*` call and must remain valid until that call completes. Destination use must follow the submitted uploads in GPU execution order.
4. Call `FrameCleanup()` at the end of the frame. Never retain a writer across commit, cleanup, another Begin or uploader disposal.

Only the threaded writer's producer methods support concurrent use. There is no safety handle or robust stale/wrong-writer validation; callers enforce ownership and synchronization. Use non-overlapping destination writes when order matters.

Staging retirement retains the upstream frame-count mechanism rather than GPU fences. Multiple Begin/commit cycles on one uploader in the same rendered frame are unsupported: commit count can advance reuse before GPU completion. This optimization does not fix that inherited lifetime contract.

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

Validation passed 13 focused Editor tests and 41 supported GPU-buffer correctness checks, plus the correctness checks attached to the repeated timing runs. Minimal-source strides and problematic page bounds were excluded only for Stock; unsafe same-frame retirement was excluded for both. GPU **output correctness** was checked; GPU timing, Player/build performance, other graphics APIs and whole-game gains remain unverified. Existing production callers were not migrated as part of this work. Profile the actual caller before adoption.

## Origin

The initial implementation copies the installed Unity Entities Graphics source at fingerprint `b573f41573f19d7e3aba1683430bbed67d333124`, with the Core namespace/resource path and public control-block allocation APIs. The compute shader is unchanged. The changes described above affect CPU staging and allocation. Unity's copyright and license remain in [Third Party Notices](../Third%20Party%20Notices.md).
