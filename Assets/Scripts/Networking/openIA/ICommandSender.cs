using System.Threading.Tasks;
using Networking.openIA.Commands;

namespace Networking.openIA
{
    public interface ICommandSender
    {
        Task Send(ICommand cmd);
    }
}