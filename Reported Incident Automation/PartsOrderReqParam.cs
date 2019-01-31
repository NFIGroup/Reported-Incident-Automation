using System.Collections.Generic;

namespace Reported_Incident_Automation
{
    class PartsOrderReqParam
    {
        public class RESTHeader
        {
            public string @xmlns { get; set; }
            public string Responsibility { get; set; }
            public string RespApplication { get; set; }
            public string SecurityGroup { get; set; }
            public string NLSLanguage { get; set; }
            public string Org_Id { get; set; }
        }

        public class POeHeaderRec
        {
            public string ORDER_TYPE { get; set; }
            public string CUSTOMER_ID { get; set; }
            public string SHIP_TO_ORG_ID { get; set; }
            public string INVOICE_TO_ORG_ID { get; set; }
            public string CLAIM_NUMBER { get; set; }
            public string PROJECT_NUMBER { get; set; }
            public string RETROFIT_NUMBER { get; set; }
        }

        public class OELineRec
        {
            public string ORDERED_ITEM { get; set; }
            public string ORDERED_ID { get; set; }
            public string ORDERED_QUANTITY { get; set; }
            public string SOURCE_TYPE { get; set; }
            public string SHIP_SET { get; set; }
        }

        public class POeLineTbl
        {
            public List<OELineRec> OE_LINE_REC { get; set; }
        }

        public class InputParameters
        {
            public POeHeaderRec P_OE_HEADER_REC { get; set; }
            public POeLineTbl P_OE_LINE_TBL { get; set; }
        }

        public class CreateASalesOrderInput
        {
            public string @xmlns { get; set; }
            public RESTHeader RESTHeader { get; set; }
            public InputParameters InputParameters { get; set; }
        }

        public class RootObject
        {
            public CreateASalesOrderInput CREATE_A_SALES_ORDER_Input { get; set; }
        }
    }
}
