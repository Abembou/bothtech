using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    namespace bothtech.Shared.Services
    {
    public interface IPlatformService
    {
        bool IsMaui { get; }
    }
    public class PlatformService : IPlatformService
    {
        public bool IsMaui { get; }
        public PlatformService(bool isMaui) => IsMaui = isMaui;
    }
}

