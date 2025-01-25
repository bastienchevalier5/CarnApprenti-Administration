using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarnApprenti
{
    public class SessionStateService
    {
        public event Action OnChange;

        public void NotifyStateChanged() => OnChange?.Invoke();
    }

}
