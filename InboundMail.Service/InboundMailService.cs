using System;
using System.Configuration;
using System.ServiceProcess;
using System.Timers;

namespace EmamiInboundMail.Service
{
    public partial class InboundMailService : ServiceBase
    {
        private Timer timer = null;
        public InboundMailService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                int intervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalMinutes"]);
                intervalMinutes = intervalMinutes * 1000;
                timer = new Timer();
                this.timer.Interval = intervalMinutes;
                this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.timer_Tick);
                timer.Enabled = true;
                timer.Start();
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("OnStart :- " + ex.Message);
            }
        }

        protected override void OnStop()
        {
            try
            {
                timer.Enabled = false;
                ErrorLog.WriteHistoryLog("InboundMailService Service stopped");
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("OnStop :- " + ex.Message);
            }
        }

        private void timer_Tick(object sender, ElapsedEventArgs e)
        {
            try
            {
                //FetchAllMails.ReadAllConfigurations();
                ErrorLog.WriteHistoryLog("InboundMailService  Service starting");
                ErrorLog.WriteHistoryLog("InboundMailService  Service started {0}" + e.SignalTime.ToString());
                InboundMailProcess.Start().Wait();
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("timer_Tick :- " + ex.InnerException);
            }
        }
    }
}
