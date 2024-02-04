using System;
using Crestron.SimplSharp;

namespace Mohammad_Hadizadeh_Certificate_Platinum.StoreFronts
{
    public class StoreFronts
    {
        private readonly StoreFront[] _storeFronts;

        public StoreFronts(int count)
        {
            _storeFronts = new StoreFront[count];
        }

        public StoreFront this[string storeId]
        {
            get
            {
                foreach (var storeFront in _storeFronts)
                {
                    if (storeFront.SpaceId == storeId)
                    {
                        return storeFront;
                    }
                }

                return null;
            }
            set
            {
                for (var i = 0; i < _storeFronts.Length; i++)
                {
                    if(_storeFronts[i] != null)
                    {
                        if (_storeFronts[i].SpaceId != storeId) continue;
                        _storeFronts[i] = value;
                        break;
                    }

                    _storeFronts[i] = value;
                    break;
                }
            }
        }
    }
    
    public class StoreFront : Space
    {
        public override ushort GetModeColor()
        {
            CrestronConsole.PrintLine($"Getting color for {SpaceMode}");
            switch (SpaceMode)
            {
                case SpaceMode.MySpace:
                    return 4;
                case SpaceMode.Available:
                    return 1;
                case SpaceMode.Occupied:
                    return 4;
                case SpaceMode.Closed:
                    return 4;
                default:
                    return 0;
            }
        }
    }
}