using System;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using vPub2023;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class CardReader
    {
        private readonly vPubID _vPubId = new vPubID();
        public static ushort CardNumber = 0;
        public static string MemberId = "";
        public static string MemberName = "";
        private static string _memberExpiryDate = "";
        public static DateTime MemberExpiryDateTime;
        public static bool MembershipIsValid = false;
        private bool _membershipDateIsValid = false;
        private bool _memberAlreadyLoggedIn = false;
        
        public event EventHandler MemberIsNotExpired = delegate { };
        protected virtual void OnMemberIsNotExpired(EventArgs e)
        {
            MemberIsNotExpired(this, e);
        }
        
        public event EventHandler MemberIsExpired = delegate { };
        protected virtual void OnMemberIsExpired(EventArgs e)
        {
            MemberIsExpired(this, e);
        }

        public string GetMemberInfo(ushort cardNumber)
        {
            var memberInfo = "";
            var member = _vPubId.QueryID(cardNumber);
            if (member != null)
            {
                memberInfo = member;
            }

            MemberInfoParser(memberInfo);

            foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
            {
                CrestronConsole.PrintLine($"Checking the member login at: {storesIpAddress}");
                var memberInquiry = new MemberInquiryRequest().GetMemberInquiryRequest(storesIpAddress.ToString());
                CrestronConsole.PrintLine($"Member Inquiry: {memberInquiry}");
                
                _memberAlreadyLoggedIn = MemberId == memberInquiry;
                if(_memberAlreadyLoggedIn) break;
            }

            if (IsMemberExpired(_memberExpiryDate))
            {
                CrestronConsole.PrintLine("Member {0} is expired.", MemberName);
                _membershipDateIsValid = false;
                
                MembershipIsValid = _membershipDateIsValid && !_memberAlreadyLoggedIn;
                OnMemberIsExpired(EventArgs.Empty);
            }
            else
            {
                _membershipDateIsValid = true;
                
                MembershipIsValid = _membershipDateIsValid && !_memberAlreadyLoggedIn;
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
            _memberExpiryDate = memberExpiryDate;
        }

        private bool IsMemberExpired(string memberExpiryDate)
        {
            var memberExpiryDateYear = int.Parse(memberExpiryDate.Substring(0, 4));
            var memberExpiryDateMonth = int.Parse(memberExpiryDate.Substring(4, 2));
            var memberExpiryDateDay = int.Parse(memberExpiryDate.Substring(6, 2));
            var memberExpiryDateDateTime =
                new DateTime(memberExpiryDateYear, memberExpiryDateMonth, memberExpiryDateDay);
            MemberExpiryDateTime = memberExpiryDateDateTime;
            var today = DateTime.Today;
            return today > memberExpiryDateDateTime;
        }
    }
}