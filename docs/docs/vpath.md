# VPath

All external paths are strings, but internally everything uses `VPath` for consistent and normalized path handling
within the virtual file system.

## What is VPath?

`VPath` is a lightweight wrapper around path strings that ensures all paths within DokiFS are normalized and handled
consistently. It automatically converts backslashes to forward slashes, removes duplicate separators, and provides
convenient methods for common path operations.

## Creating VPaths

```csharp
// From string literals
VPath path = "/home/user/documents";

// Explicit construction
VPath path = new VPath("C:\\Users\\John\\Documents");
// Automatically normalized to: "C:/Users/John/Documents"

// Common paths
VPath root = VPath.Root;        // "/"
VPath empty = VPath.Empty;      // ""
```

## Basic Path Operations

### Combining Paths

Use the `/` operator to combine paths safely:

```csharp
VPath basePath = "/home/user";
VPath fileName = "document.txt";
VPath fullPath = basePath / fileName;
// Result: "/home/user/document.txt"

// Also works with strings
VPath configPath = basePath / "config" / "settings.json";
// Result: "/home/user/config/settings.json"

// Alternatively, use the append method
VPath filePath = basePath.Append("file.txt");
```

### Checking Path Properties

```csharp
VPath path = "/home/user/documents/";

// Check path type
bool isRoot = path.IsRoot;           // false
bool isAbsolute = path.IsAbsolute;   // true
bool isDirectory = path.IsDirectory; // true (ends with /)
bool isEmpty = path.IsEmpty;         // false

// Check for hidden files/folders
VPath hiddenFile = "/home/user/.bashrc";
bool isHidden = hiddenFile.IsHidden; // true
```

## Working with Path Segments

### Splitting Paths

```csharp
VPath path = "/home/user/documents";
string[] segments = path.Split();
// Result: ["home", "user", "documents"]

// Useful for iterating through path components
foreach (string segment in path.Split())
{
    Console.WriteLine($"Segment: {segment}");
}
```

### Path Prefix Operations

```csharp
VPath fullPath = "/var/www/html/index.html";
VPath basePath = "/var/www";

// Check if path starts with another path
bool isUnderBase = fullPath.StartsWith(basePath); // true

// Remove a prefix from the path
VPath relativePath = fullPath.ReduceStart(basePath);
// Result: "/html/index.html"
```

### Getting parts of the path
```csharp
VPath filePath = "/home/user/documents/file.txt";

// Get the containing directory
VPath directory = filePath.GetDirectory();
// Result: "/home/user/documents"

// Get the filename
VPath fileName = filePath.GetFileName()
// Result: "file.txt"

// Get the first segment of the path
string rootDir = filePath.GetRoot();
// Result: "/home"

// Get the final segment of the path
string leaf = filePath.GetLeaf();
// Result: "file.txt"

// Split the path into segments
string[] segments = filePath.Split();
```
