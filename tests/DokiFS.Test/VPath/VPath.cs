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
        Assert.False(path.IsNull);
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

        Assert.Equal("test/path/", path4.ToString());
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
}
