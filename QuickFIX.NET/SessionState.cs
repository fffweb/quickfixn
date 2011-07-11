﻿using System.Collections.Generic;

namespace QuickFix
{
    public class SessionState : MessageStore
    {
        #region Private Members

        private object sync_ = new object();
        private bool isEnabled_ = true;
        private bool receivedLogon_ = false;
        private bool receivedReset_ = false;
        private bool sentLogon_ = false;
        private bool sentReset_ = false;
        private string logoutReason_ = "";
        private int testRequestCounter_ = 0;
        private int nextSenderMsgSeqNum_ = 1;
        private int heartBtInt_ = 0;
        private int heartBtIntAsTickCount_ = 0;
        private int lastReceivedTimeTickCount_;
        private int lastSentTimeTickCount_;

        private Log log_;

        #endregion

        #region Unsynchronized Properties

        public bool IsInitiator
        { get; set; }

        public bool ShouldSendLogon
        { get { return IsInitiator && !SentLogon; } }

        public Log Log
        { get { return log_; } }

        #endregion

        #region Synchronized Properties

        public bool IsEnabled
        {
            get { lock (sync_) { return isEnabled_; } }
            set { lock (sync_) { isEnabled_ = value; } }
        }

        public bool ReceivedLogon
        {
            get { lock (sync_) { return receivedLogon_; } }
            set { lock (sync_) { receivedLogon_ = value; } }
        }

        public bool ReceivedReset
        {
            get { lock (sync_) { return receivedReset_; } }
            set { lock (sync_) { receivedReset_ = value; } }
        }

        public bool SentLogon
        {
            get { lock (sync_) { return sentLogon_; } }
            set { lock (sync_) { sentLogon_ = value; } }
        }

        public bool SentReset
        {
            get { lock (sync_) { return sentReset_; } }
            set { lock (sync_) { sentReset_ = value; } }
        }

        public string LogoutReason
        {
            get { lock (sync_) { return logoutReason_; } }
            set { lock (sync_) { logoutReason_ = value; } }
        }

        public int TestRequestCounter
        {
            get { lock (sync_) { return testRequestCounter_; } }
            set { lock (sync_) { testRequestCounter_ = value; } }
        }

        public int HeartBtInt
        {
            get { lock (sync_) { return heartBtInt_; } }
            set { lock (sync_) { heartBtInt_ = value; heartBtIntAsTickCount_ = 1000 * value;  } }
        }

        public int HeartBtIntAsTickCount
        {
            get { lock (sync_) { return heartBtIntAsTickCount_; } }
        }

        public int LastReceivedTimeTickCount
        {
            get { lock (sync_) { return lastReceivedTimeTickCount_; } }
            set { lock (sync_) { lastReceivedTimeTickCount_ = value; } }
        }

        public int LastSentTimeTickCount
        {
            get { lock (sync_) { return lastSentTimeTickCount_; } }
            set { lock (sync_) { lastSentTimeTickCount_ = value; } }
        }

        public int NextSenderMsgSeqNum
        {
            get { lock (sync_) { return nextSenderMsgSeqNum_; } }
        }

        #endregion

        public SessionState(Log log, int heartBtInt)
        {
            log_ = log;
            this.IsInitiator = (0 != heartBtInt);
            this.HeartBtInt = heartBtInt;
            int now = System.Environment.TickCount;
            lastReceivedTimeTickCount_ = now;
            lastSentTimeTickCount_ = now;
        }

        /// <summary>
        /// FIXME
        /// </summary>
        public void Reset()
        {
            // messageStore_.Reset();
        }

        /// <summary>
        /// FIXME
        /// </summary>
        /// <returns></returns>
        public bool LogonTimedOut()
        {
            return false;
        }

        /// <summary>
        /// All time args are in milliseconds
        /// </summary>
        /// <param name="now">current system time in milliseconds</param>
        /// <param name="heartBtIntMillis">heartbeat interval in milliseconds</param>
        /// <param name="lastReceivedTime">last received time in milliseconds</param>
        /// <returns>true if timed out</returns>
        public static bool TimedOut(int now, int heartBtIntMillis, int lastReceivedTime)
        {
            int nowTickCount = System.Environment.TickCount;
            int elapsed = now - lastReceivedTime;
            return elapsed >= (2.4 * heartBtIntMillis);
        }
        public bool TimedOut()
        {
            return TimedOut(System.Environment.TickCount, this.HeartBtIntAsTickCount, this.LastReceivedTimeTickCount);
        }

        /// <summary>
        /// All time args are in milliseconds
        /// </summary>
        /// <param name="now">current system time in milliseconds</param>
        /// <param name="heartBtIntMillis">heartbeat interval in milliseconds</param>
        /// <param name="lastReceivedTime">last received time in milliseconds</param>
        /// <param name="testRequestCounter">test request counter</param>
        /// <returns>true if test request is needed</returns>
        public static bool NeedTestRequest(int now, int heartBtIntMillis, int lastReceivedTime, int testRequestCounter)
        {
            int elapsedTickCount = now - lastReceivedTime;
            return elapsedTickCount >= (1.2 * ((testRequestCounter + 1) * heartBtIntMillis));
        }
        public bool NeedTestRequest()
        {
            return NeedTestRequest(System.Environment.TickCount, this.HeartBtIntAsTickCount, this.LastReceivedTimeTickCount, this.TestRequestCounter);
        }

        /// <summary>
        /// All time args are in milliseconds
        /// </summary>
        /// <param name="now">current system time in milliseconds</param>
        /// <param name="heartBtIntMillis">heartbeat interval in milliseconds</param>
        /// <param name="lastSentTime">last sent time in milliseconds</param>
        /// <param name="testRequestCounter">test request counter</param>
        /// <returns>true if heartbeat is needed</returns>
        public static bool NeedHeartbeat(int now, int heartBtIntMillis, int lastSentTime, int testRequestCounter)
        {
            int elapsed = now - lastSentTime;
            return (elapsed >= heartBtIntMillis) && (0 == testRequestCounter);
        }
        public bool NeedHeartbeat()
        {
            return NeedHeartbeat(System.Environment.TickCount, this.HeartBtIntAsTickCount, this.LastSentTimeTickCount, this.TestRequestCounter);
        }

        /// <summary>
        /// All time args are in milliseconds
        /// </summary>
        /// <param name="now">current system time in milliseconds</param>
        /// <param name="heartBtIntMillis">heartbeat interval in milliseconds</param>
        /// <param name="lastSentTime">last sent time in milliseconds</param>
        /// <param name="lastReceivedTime">last received time in milliseconds</param>
        /// <returns>true if within heartbeat interval</returns>
        public static bool WithinHeartbeat(int now, int heartBtIntMillis, int lastSentTime, int lastReceivedTime)
        {
            return ((now - lastSentTime) < heartBtIntMillis)
                && ((now - lastReceivedTime) < heartBtIntMillis);
        }
        public bool WithinHeartbeat()
        {
            return WithinHeartbeat(System.Environment.TickCount, this.HeartBtIntAsTickCount, this.LastSentTimeTickCount, this.LastReceivedTimeTickCount);
        }
        
        public void IncrNextTargetMsgSeqNum()
        {
            lock (sync_)
            {
                ///FIXME
            }
        }

        /// <summary>
        /// All time values are displayed in milliseconds.
        /// </summary>
        /// <returns>a string that represents the session state</returns>
        public override string ToString()
        {
            return new System.Text.StringBuilder("SessionState ")
                .Append("[ Now=").Append(System.Environment.TickCount)
                .Append(", HeartBtInt=").Append(this.HeartBtIntAsTickCount)
                .Append(", LastSentTime=").Append(this.LastSentTimeTickCount)
                .Append(", LastReceivedTime=").Append(this.LastReceivedTimeTickCount)
                .Append(", TestRequestCounter=").Append(this.TestRequestCounter)
                .Append(", WithinHeartbeat=").Append(WithinHeartbeat())
                .Append(", NeedHeartbeat=").Append(NeedHeartbeat())
                .Append(", NeedTestRequest=").Append(NeedTestRequest())
                .Append(" ]").ToString();
            
        }
    }
}