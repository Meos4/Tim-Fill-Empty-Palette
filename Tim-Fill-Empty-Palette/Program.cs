namespace Tim_Fill_Empty_Palette;

class Program
{
	static void Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.WriteLine("Please provide a file path as an argument.");
			return;
		}

		var pattern = new byte[] { 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00 };

		foreach (var arg in args)
		{
			try
			{
				using (RawFile file = new RawFile(arg))
				{
					// 512 Mo max
					if (file.Size > 0x20000000)
					{
						throw new Exception("File is too large.");
					}

					var buffer = file.Read();
					var offsets = Pattern.OccurenceOffsets(buffer, pattern, 4, 9);
					var timHeaders = new List<TimHeader>();
					var toAddNewBuffer = 0;

					for (int i = offsets.Count - 1; i >= 0; --i)
					{
						var viewer = new ReadOnlySpan<byte>(buffer, offsets[i], TimHeader.HeaderSize);
						var timHeader = TimHeader.Create(viewer);

						if (timHeader == null)
						{
							offsets.RemoveAt(i);
							continue;
						}

						if (timHeader.IsEmptyPalette())
						{
							toAddNewBuffer += (int)timHeader.ExpectedPaletteSize();
						}

						timHeaders.Add(timHeader);
					}

					if (toAddNewBuffer == 0)
					{
						continue;
					}

					timHeaders.Reverse();

					var newBuffer = new byte[file.Size + toAddNewBuffer];
					var lastPaletteViewer = ReadOnlySpan<byte>.Empty;
					var previousOffset = 0;
					var shiftNewBuffer = 0;

					foreach (var (offset, header) in offsets.Zip(timHeaders))
					{
						var toFill = offset - previousOffset + TimHeader.HeaderSize;
						Buffer.BlockCopy(buffer, previousOffset, newBuffer, previousOffset + shiftNewBuffer, toFill);
						previousOffset = offset + TimHeader.HeaderSize;

						if (!header.IsEmptyPalette())
						{
							lastPaletteViewer = new ReadOnlySpan<byte>(buffer, offset + TimHeader.HeaderSize, (int)header.PaletteSize());
						}
						else
						{
							if (lastPaletteViewer.IsEmpty)
							{
								throw new Exception($"No valid palette found for offset 0x{offset:X}.");
							}

							var newBufferOffset = offset + shiftNewBuffer;

							// Palette offset
							var paletteOffsetSpan = new Span<byte>(newBuffer, newBufferOffset + sizeof(uint) * 2, sizeof(uint));
							BitConverter.TryWriteBytes(paletteOffsetSpan, (uint)(lastPaletteViewer.Length + TimHeader.DefaultPaletteOffset));

							// Palette
							Buffer.BlockCopy(lastPaletteViewer.ToArray(), 0, newBuffer, newBufferOffset + TimHeader.HeaderSize, lastPaletteViewer.Length);

							shiftNewBuffer += lastPaletteViewer.Length;
						}
					}

					// Remaining data
					Buffer.BlockCopy(buffer, previousOffset, newBuffer, previousOffset + shiftNewBuffer, buffer.Length - previousOffset);
					file.Write(newBuffer);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading file {arg}: {ex.Message}");
			}
		}
	}
}