using System.IO;
using System.Threading.Tasks;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command("cwd")]
public partial class WorkingDirectoryCommand : ICommand
{
    public async ValueTask ExecuteAsync(IConsole console) =>
        await console.Output.WriteLineAsync(Directory.GetCurrentDirectory());
}
