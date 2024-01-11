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
        public static string MemberId = "";
        public static string MemberName = "";
        public static string MemberExpiryDate = "";
        
        public event EventHandler MemberIsNotExpired = delegate { };
        protected virtual void OnMemberIsNotExpired(EventArgs e)
        {
            MemberIsNotExpired(this, e);
        }

        public string GetMemberInfo(ushort cardNumber)
        {
            var memberInfo = "";
            var member = _vPubID.QueryID(cardNumber);
            if (member != null)
            {
                memberInfo = member;
            }

            MemberInfoParser(memberInfo);
            if (IsMemberExpired(MemberExpiryDate))
            {
                CrestronConsole.PrintLine("Member {0} is expired.", MemberName);
            }
            else
            {
                OnMemberIsNotExpired(EventArgs.Empty);
            }

            return memberInfo;
        }

        private void MemberInfoParser(string memberInfo)
        {
            if (string.IsNullOrEmpty(memberInfo)) return;
            var memberInfoArray = memberInfo.Split('|');
            if (memberInfoArray.Length < 3) return;
            var memberId = memberInfoArray[0];
            var memberName = memberInfoArray[1];
            var memberExpiryDate = memberInfoArray[2];
            MemberId = memberId;
            MemberName = memberName;
            MemberExpiryDate = memberExpiryDate;
        }

        private bool IsMemberExpired(string memberExpiryDate)
        {
            var memberExpiryDateYear = int.Parse(memberExpiryDate.Substring(0, 4));
            var memberExpiryDateMonth = int.Parse(memberExpiryDate.Substring(4, 2));
            var memberExpiryDateDay = int.Parse(memberExpiryDate.Substring(6, 2));
            var memberExpiryDateDateTime =
                new DateTime(memberExpiryDateYear, memberExpiryDateMonth, memberExpiryDateDay);
            var today = DateTime.Today;
            return today > memberExpiryDateDateTime;
        }
    }
}