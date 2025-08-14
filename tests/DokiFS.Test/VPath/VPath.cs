namespace DokiFS.Tests.VPathTests;

public class VPathTests
{
    [Fact(DisplayName = "VPath: Basic creation")]
    public void VPathCreation()
    {
        VPath path = new("/test/path");

        Assert.Equal("/test/path", path.ToString());
        Assert.True(path.IsAbsolute);
        Assert.False(path.IsEmpty);
    }

    [Fact(DisplayName = "VPath: backslash handling")]
    public void BackslashHandling()
    {
        VPath path1 = new("/test\\path");
        VPath path2 = new("\\test/path");
        VPath path3 = new("\\test\\path");
        VPath path4 = new("test/path/");

        Assert.Equal("/test/path", path1.ToString());
        Assert.Equal("/test/path", path2.ToString());
        Assert.Equal("/test/path", path3.ToString());
        Assert.NotEqual("/test/path/", path4.ToString());
    }

    [Fact(DisplayName = "VPath: Relative path")]
    public void RelativePathHandling()
    {
        VPath path4 = new("test/path/");

        Assert.Equal("test/path", path4.ToString());
        Assert.False(path4.IsAbsolute);
    }

    [Fact(DisplayName = "VPath: long path (256+ characters) handling")]
    public void LongPathHandling()
    {
        string longPath = "/" + new string('a', 300);
        VPath path = new(longPath);

        Assert.Equal(longPath, path.ToString());

        string longPathWithBackslashes = "\\" + new string('a', 300) + "\\foo";
        string expectedNormalized = "/" + new string('a', 300) + "/foo";
        VPath normalizedPath = new(longPathWithBackslashes);

        Assert.Equal(expectedNormalized, normalizedPath.ToString());
    }

    [Fact]
    public void EmptyShouldReturnTrue()
    {
        VPath path = VPath.Empty;

        Assert.True(path.IsEmpty);
    }

    [Fact]
    public void IsRootShouldReturnTrue()
    {
        VPath path = "/";

        Assert.True(path.IsRoot);
        Assert.False(path.IsEmpty);
        Assert.Equal(1, path.FullPath.Length);
        Assert.Equal(VPath.DirectorySeparator, path.FullPath[0]);
    }

    [Fact]
    public void IsAbsoluteShouldReturnTrue()
    {
        VPath path = "/test/path";

        Assert.True(path.IsAbsolute);
    }

    [Fact]
    public void ShouldAppend()
    {
        VPath a = "/test";
        VPath b = "path";
        VPath c = a.Append(b);

        Assert.Equal("/test/path", c.ToString());
    }

    [Fact]
    public void StartsWithShouldReturnTrue()
    {
        VPath a = "/test/path";
        VPath b = "/test";

        Assert.True(a.StartsWith(b));
    }

    [Fact]
    public void GetDirectoryShouldReturnParentDirectory()
    {
        VPath a = "/test/path";
        VPath b = "/test/file.ext";
        VPath expected = "/test/";

        Assert.Equal(expected, a.GetDirectory());
        Assert.Equal(expected, b.GetDirectory());
    }

    [Fact]
    public void ShouldReturnCorrectLeaf()
    {
        VPath path1 = "/a/b/c/d/file.txt";
        VPath path2 = "/a/b/c";

        Assert.Equal("file.txt", path1.GetLeaf());
        Assert.Equal("c", path2.GetLeaf());
    }

    [Fact]
    public void ShouldReturnCorrectLeafSpecialCases()
    {
        VPath path1 = "/test";
        VPath path2 = "/";

        Assert.Equal("test", path1.GetLeaf());
        Assert.Equal("/", path2.GetLeaf());
    }

    [Fact]
    public void ShouldReturnFileName()
    {
        VPath filepath = "/test/file.txt";
        VPath dirpath = "/test";
        VPath emptyPath1 = "";
        VPath emptyPath2 = new();
        VPath emptyPath3 = VPath.Empty;

        Assert.Equal("file.txt", filepath.GetFileName());
        Assert.Equal(string.Empty, dirpath.GetFileName());
        Assert.Equal(string.Empty, emptyPath1.GetFileName());
        Assert.Equal(string.Empty, emptyPath2.GetFileName());
        Assert.Equal(string.Empty, emptyPath3.GetFileName());
    }

    [Fact]
    public void SplitShouldReturnSegments()
    {
        VPath path = "/test/path/with/segments";
        VPath zeroPath = "/";

        string[] segments = path.Split();
        string[] zeroSegments = zeroPath.Split();

        Assert.Equal(4, segments.Length);
        Assert.Equal("test", segments[0]);
        Assert.Equal("path", segments[1]);
        Assert.Equal("with", segments[2]);
        Assert.Equal("segments", segments[3]);

        Assert.Empty(zeroSegments);
    }

    [Fact]
    public void GoingUp()
    {
        VPath path = "/a/b/c/d/e";
        VPath pathWithFile = "/a/b/c/d/e/file.txt";

        VPath upPath = path.Up();
        VPath upPathWithFile = pathWithFile.Up();

        Assert.Equal("/a/b/c/d", upPath.ToString());
        Assert.Equal("/a/b/c/d", upPathWithFile.ToString());
    }

    [Fact]
    public void UpShouldStopAtRootForAbsolutePaths()
    {
        VPath absPath = "/a";
        VPath relPath = "a";
        VPath nullPath = null;
        VPath emptyPath = VPath.Empty;
        VPath rootPath = VPath.Root;

        Assert.Equal(VPath.Root, absPath.Up());
        Assert.Equal(VPath.Root, rootPath.Up());
        Assert.Equal(VPath.Empty, relPath.Up());
        Assert.Equal(VPath.Empty, nullPath.Up());
        Assert.Equal(VPath.Empty, emptyPath.Up());
    }

    [Fact]
    public void ShouldReducePathFromStart()
    {
        VPath path1 = "/test/a/b/c";
        VPath reduction1 = "/test/";
        VPath expected1 = "/a/b/c";

        VPath path2 = "/test/a/b/c";
        VPath reduction2 = "/a/b/c/";
        VPath expected2 = "/test/";

        Assert.Equal(expected1, path1.ReduceStart(reduction1));
        Assert.NotEqual(expected2, path2.ReduceStart(reduction2));
    }

    [Fact]
    public void ShouldReducePathFromEnd()
    {
        VPath path1 = "/test/a/b/c";
        VPath reduction1 = "/test/";
        VPath expected1 = "/a/b/c";

        VPath path2 = "/test/a/b/c";
        VPath reduction2 = "/a/b/c/";
        VPath expected2 = "/test/";

        Assert.NotEqual(expected1, path1.ReduceEnd(reduction1));
        Assert.Equal(expected2, path2.ReduceEnd(reduction2));
    }
}
