using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.Core.Abstractions.Data
{
    public interface IDataLoader
    {
        T Load<T>();
    }
}
