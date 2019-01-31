using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using RightNow.AddIns.AddInViews;

namespace Reported_Incident_Automation
{
    class ExpirationDate
    {
        IRecordContext _recordContext;
        DateTime _expirationDate = DateTime.Now;

        public ExpirationDate(IRecordContext RecordContext)
        {
            _recordContext = RecordContext;
        }

        public void SetExpirationDate()
        {
            try
            {
                string fsarTimeLimit = "";
                string offset;
                string duration;

                //Get FSAR time limit value
                IGenericObject fsar = (IGenericObject)_recordContext.GetWorkspaceRecord("CO$FSAR");
                IIncident incident = (IIncident)_recordContext.GetWorkspaceRecord(RightNow.AddIns.Common.WorkspaceRecordType.Incident);
                foreach (IGenericField genField in fsar.GenericFields)
                {
                    if (genField.Name == "time_limit" && genField.DataValue.Value != null)
                    {
                        fsarTimeLimit = genField.DataValue.Value.ToString();
                        break;
                    }
                }

                if (fsarTimeLimit != "")
                {
                    offset = fsarTimeLimit.Split(' ')[0];
                    duration = fsarTimeLimit.Split(' ')[1];

                    //Check for adding Days to current date
                    if (duration.Contains("D") || duration.Contains("d"))
                    {
                        _expirationDate = DateTime.Now.AddDays(Convert.ToDouble(offset));
                    }

                    //Check for adding Weeks to current date
                    if (duration.Contains("W") || duration.Contains("w"))
                    {
                        double days = Convert.ToDouble(offset);
                        //Convert weeks to days
                        days = days * 7;
                        _expirationDate = DateTime.Now.AddDays(Convert.ToDouble(days));
                    }

                    //Check for adding Months to current date
                    if (duration.Contains("M") || duration.Contains("m"))
                    {
                        _expirationDate = DateTime.Now.AddMonths(Convert.ToInt32(offset));
                    }

                   RightNowConnectService.GetService().setIncidentField("CO", "expiration_date", _expirationDate.ToString(),incident);
                    /*foreach (ICustomAttribute customAttribute in incident.CustomAttributes)
                    {
                        if (customAttribute.GenericField.Name == "CO$expiration_date")
                        {
                            customAttribute.GenericField.DataValue.Value = (object)_expirationDate;
                            break;
                        }
                    }*/
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in Setting Expiration Date: " + ex.Message);
            }
        }
    }
}

