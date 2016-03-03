using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixedThreadPool
{
    public interface ITask
    {
        void Execute();
    }
}
