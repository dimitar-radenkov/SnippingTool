namespace SnippingTool.Services;

public interface IVideoWriterFactory
{
    IVideoWriter Create(int width, int height, int fps, string outputPath);
}
