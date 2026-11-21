using System;
using System.Buffers;
using System.Threading.Tasks;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using CliWrap.Tests.Dummy.Commands.Shared;

namespace CliWrap.Tests.Dummy.Commands;

[Command("generate binary")]
public partial class GenerateBinaryCommand : ICommand
{
    // Tests rely on the random seed being fixed
    private readonly Random _random = new(1234567);

    [CommandOption("target")]
    public OutputTarget Target { get; set; } = OutputTarget.StdOut;

    [CommandOption("length")]
    public long Length { get; set; } = 100_000;

    [CommandOption("buffer")]
    public int BufferSize { get; set; } = 1024;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent(BufferSize);

        var totalBytesGenerated = 0L;
        while (totalBytesGenerated < Length)
        {
            _random.NextBytes(buffer.Memory.Span);

            var bytesWanted = (int)Math.Min(buffer.Memory.Length, Length - totalBytesGenerated);

            foreach (var writer in console.GetWriters(Target))
                await writer.BaseStream.WriteAsync(buffer.Memory[..bytesWanted]);

            totalBytesGenerated += bytesWanted;
        }
    }
}
