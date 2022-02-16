using System.Threading.Tasks;

namespace Multitool.Data
{
    public delegate void TaskCompletedEventHandler(TaskStatus status, Task completedTask = null);
}
