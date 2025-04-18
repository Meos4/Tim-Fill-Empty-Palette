namespace Tim_Fill_Empty_Palette;

public class RawFile : IDisposable
{
	private readonly FileStream _stream;
	public long Size => _stream.Length;

	public RawFile(string path)
	{
		_stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
	}

	public byte[] Read()
	{
		return Read(0, (int)Size);
	}

	public byte[] Read(long offset, int count)
	{
		var buffer = new byte[count];
		_stream.Seek(offset, SeekOrigin.Begin);
		var bytesRead = _stream.Read(buffer, 0, count);

		if (bytesRead != count)
		{
			Array.Resize(ref buffer, bytesRead);
		}

		return buffer;
	}

	public void Write(byte[] data)
	{
		Write(0, data);
	}

	public void Write(long offset, byte[] data)
	{
		_stream.Seek(offset, SeekOrigin.Begin);
		_stream.Write(data, 0, data.Length);
	}

	public void Dispose()
	{
		_stream.Dispose();
	}
}