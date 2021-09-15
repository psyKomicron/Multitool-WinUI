using System.Threading.Tasks;

namespace Multitool.DAL
{
    public delegate void TaskCompletedEventHandler(TaskStatus status, Task completedTask = null);
}
