using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using vPub2023;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class CardReader
    {
        private vPubID _vPubID = new vPubID();
        public static ushort CardNumber = 0;
        
        public string GetMemberInfo(ushort cardNumber)
        {
            var memberInfo = "";
            var member = _vPubID.QueryID(cardNumber);
            if (member != null)
            {
                memberInfo = member;
            }
            return memberInfo;
        }
    }
}