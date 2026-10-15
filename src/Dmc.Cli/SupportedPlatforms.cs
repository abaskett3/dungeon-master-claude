using System.Runtime.Versioning;

// dmc ships for exactly these three. Saying so lets the platform analyzer (CA1416) accept PDFtoImage, whose API is
// marked for them and a few others, and still warn about any call that would work on only one of them.
[assembly: SupportedOSPlatform("windows")]
[assembly: SupportedOSPlatform("linux")]
[assembly: SupportedOSPlatform("macos")]
