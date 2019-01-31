using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RightNow.AddIns.AddInViews;
using System.Web.Script.Serialization;
using static Reported_Incident_Automation.SupplierWarrantyRequiredParams;

namespace Reported_Incident_Automation
{
    class SupplierWarrantyCheckModel
    {
        
        string _xmlnsURL;
        string _curlURL;
        string _headerURL;
        string _responsibility;
        string _respApplication;
        string _nlsLanguage;
        string _securityGroup;
        string _orgID;

        string _supplierID = String.Empty;
        string _causalPartNumber = String.Empty;
        string _vin = String.Empty;
        string _odometerReading = String.Empty;
        string _failedDate = String.Empty;
        public static RightNowConnectService _rnConnectService;
        IIncident _incidentRecord;


        public SupplierWarrantyCheckModel()
        {
            _rnConnectService = RightNowConnectService.GetService();
            string supplierWarrantyConfigValue = _rnConnectService.GetConfigValue("CUSTOM_CFG_SUPPLIER_WARRANTY");
            if (supplierWarrantyConfigValue != null)
            {
                var s = new JavaScriptSerializer();
                var configVerb = s.Deserialize<WebServiceConfigVerbs.RootObject>(supplierWarrantyConfigValue);
                _curlURL = configVerb.URL;
                _xmlnsURL = configVerb.xmlns;
                _headerURL = configVerb.RESTHeader.xmlns;
                _respApplication = configVerb.RESTHeader.RespApplication;
                _responsibility = configVerb.RESTHeader.Responsibility;
                _securityGroup = configVerb.RESTHeader.SecurityGroup;
                _nlsLanguage = configVerb.RESTHeader.NLSLanguage;
                _orgID = configVerb.RESTHeader.Org_Id;
            }
        }

        /// <summary>
        /// Get required details to build WebRequest
        /// </summary>
        public void GetDetails(IIncident incidentRecord, IRecordContext recordContext)
        {
            _incidentRecord = incidentRecord;
            //Get the VIN Number
            string[] vinDetails = RightNowConnectService.GetService().getBusInfo(_incidentRecord.ID);
            if (vinDetails != null)
            {
                _vin = vinDetails[0].Split('~')[0];
            }

            //Get OOTB ORG ID
            _supplierID =RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "supplier_from_webservice", _incidentRecord);
            if (_supplierID == String.Empty)
            {
                WorkspaceAddIn.InfoLog("Supplier ID is blank");
                return ;
            }
            else
            {
                //Get EBS ORG ID
                _supplierID = RightNowConnectService.GetService().GetEbsOrgID(Convert.ToInt32(_supplierID));
            }
            _causalPartNumber = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "causal_part_nmbr", _incidentRecord);
            if (_causalPartNumber == String.Empty)
            {
                WorkspaceAddIn.InfoLog("Causal Part Number is blank");
                return ;
            }
            _odometerReading = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "odometer_reading", _incidentRecord);
            if (_odometerReading == String.Empty)
            {
                WorkspaceAddIn.InfoLog("Odometer Reading is blank");
                return ;
            }
            _failedDate = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "failure_date", _incidentRecord);
            if (_failedDate == String.Empty)
            {
                WorkspaceAddIn.InfoLog("Failed Date is blank");
                return ;
            }
            //If all required info is valid then form jSon request parameter
            var content = GetReqParam();
            var jsonContent = WebServiceRequest.JsonSerialize(content);
            jsonContent = jsonContent.Replace("xmlns", "@xmlns");

            //Call webservice                 
            string jsonResponse = WebServiceRequest.Get(_curlURL, jsonContent, "POST");
            if (jsonResponse == "")
            {
                WorkspaceAddIn.InfoLog("Server didn't return any info");
            }
            else
            {
                ExtractResponse(jsonResponse);
            }
            return;
        }

        /// <summary>
        /// Fucntion to Send Request and Receive Response
        /// </summary>
        public RootObject GetReqParam()
        {
            RootObject rootObject = new RootObject
            {
                ISPARTWARRANTABLE_Input = new ISPARTWARRANTABLEInput
                {
                    xmlns = _xmlnsURL,
                    RESTHeader = new RESTHeader
                    {
                        xmlns = _headerURL,
                        Responsibility = _responsibility,
                        RespApplication = _respApplication,
                        SecurityGroup = _securityGroup,
                        NLSLanguage = _nlsLanguage,
                        Org_Id = _orgID
                    },
                    InputParameters = new InputParameters
                    {
                        P_SUPPLIER_ID = Convert.ToInt32(_supplierID),
                        P_PART = _causalPartNumber,
                        P_MILES = Convert.ToInt32(_odometerReading),
                        P_VIN = _vin,
                        P_FAILDATE = Convert.ToDateTime(_failedDate).ToString("dd-MMM-yyyy")
                    }
                }
            };
            return rootObject;
        }
        /// <summary>
        /// Funtion to handle ebs webservice response
        /// </summary>
        /// <param name="respJson">response in jSON string</param>
        public void ExtractResponse(string jsonResp)
        {
            int _internalIncidentID = 0;
            string[] internalIncident = RightNowConnectService.GetService().GetAllInternalIncident(_incidentRecord.ID);
            if (internalIncident != null)
            {
                _internalIncidentID = Convert.ToInt32(internalIncident[0]);
            }
                //Extract response
                Dictionary<string, object> data = (Dictionary<string, object>)WebServiceRequest.JsonDeSerialize(jsonResp);
            Dictionary<String, object> outputParameters = (Dictionary<String, object>)data["OutputParameters"];
            Dictionary<String, object> output = (Dictionary<String, object>)outputParameters["CIN_SUPPLIER_WARRANTY_PKG-24ISP"];

            if (output["ISCOVERED"].ToString() == "1")
            {
                //autogenerate sClaim
                //_rnConnectService.CreatesClaim(_incidentRecord.ID);

                RightNowConnectService.GetService().UpdateInternalIncidentForSupplier(_internalIncidentID, true, output["DESCRIPTION"].ToString());

                //Create a child object to store Supplier recoverable limit
                _rnConnectService.CreateSupplierInfo(_incidentRecord.ID,
                                                                          output["DESCRIPTION"].ToString(),
                                                                          output["TEMPLATE_NAME"].ToString(),
                                                                          output["PARTS_REIMB_PERC"].ToString(),
                                                                          output["PARTS_MAX_AMOUNT"].ToString(),
                                                                          output["LABOUR_REIMB_PERC"].ToString(),
                                                                          output["LABOUR_MAX_AMOUNT"].ToString(),
                                                                          output["OTHER_REIMB_PERC"].ToString(),
                                                                          output["OTHER_MAX_AMOUNT"].ToString()
                                                                          );
            }
            else
            {
                RightNowConnectService.GetService().UpdateInternalIncidentForSupplier(_internalIncidentID, false, output["DESCRIPTION"].ToString());
            }
        }
    }
}
