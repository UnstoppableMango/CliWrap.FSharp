using System.Threading.Tasks;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CliWrap.Tests.Dummy.Commands;

[Command]
public partial class DefaultCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console) => default;
}
