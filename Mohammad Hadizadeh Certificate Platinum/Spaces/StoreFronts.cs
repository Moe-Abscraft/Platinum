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
                    return 0;
                case SpaceMode.Occupied:
                    switch (SpaceId)
                    {
                        case "A":
                            return 2;
                        case "B":
                            return 3;
                        case "C":
                            return 5;
                        case "D":
                            return 6;
                        case "E":
                            return 7;
                        case "F":
                            return 8;
                        default:
                            return 0;
                    }

                case SpaceMode.Closed:
                    return 1;
                default:
                    return 0;
            }
        }
    }
}