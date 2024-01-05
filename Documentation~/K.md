# K
## Summary
K is an Enum or [LayerMask](https://docs.unity3d.com/ScriptReference/LayerMask.html) alternative that allows you to define your key-value pairs in setting files.
It provides a way to convert human-readable strings into values, even within burst jobs.

In particular, it's useful as an enum replacement when you want to make the values extendable across multiple libraries and projects.

## Setup
Define your Settings file.
```cs
    public class ClientStates : KSettings<ClientStates>
    {
    }
```

In Unity, open `BovineLabs -> Settings` to automatically create the associated asset.

![K](Images/K.png)

Then simply assign key-value (string, int) pairs. The maximum key length is a UTF8 string of length 15, and the maximum number of elements is 256.

## Runtime
To get your state at runtime from anywhere, including in a burst job, just use:
```cs
var state = K<ClientStates>.NameToKey("menu")
```

You can also do it in reverse, which can be useful for debugging:
```cs
var name = K<ClientStates>.KeyToName(2)
```
