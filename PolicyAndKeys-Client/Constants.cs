namespace AADB2C.PolicyAndKeys.Client
{
    public class Constants
    {
        // TODO: update "ClientIdForUserAuthn" with your app guid and "Tenant" with your tenant name
        //       see README.md for instructions
        
        // Client ID is the application guid used uniquely identify itself to the v2.0 authentication endpoint
        public static string ClientIdForUserAuthn = "ENTER_YOUR_CLIENT_ID";
        // Your tenant Name, for example "myb2ctenant.onmicrosoft.com"
        public static string Tenant = "ENTER_YOUR_TENANT_NAME";

        public static string CreateKeyset = @"{  ""id"": ""keyset1"" } ";

        public static string UpdateKeyset = @"{
                                                     ""keys"": [
                                                            {
                                                             ""k"": ""{0}"",
                                                             ""use"": ""sig"",
                                                             ""kty"": ""oct"",
                                                             ""e"": ""sjdn"",    
                                                             ""n"": ""sldssmdnsdlfmsl"" 
                                                             }
                                                         ]
                                                }";

        //((DateTimeOffset)foo).ToUnixTimeSeconds();
        public static string GenerateKey = @"{  ""use"": ""sig"",  ""kty"": ""RSA"",  ""nbf"": ""1508969811"",  ""exp"": ""1508973711"", } ";

        public static string UploadSecret = @"{  ""use"": ""sig"",  ""k"": ""secret"",  ""nbf"": ""1508969811"",  ""exp"": ""1508973711"", } ";

        public static string UploadCertificate = @"{  ""key"": ""sdkalsdasdlasdlvasdasdbvlabdlv"" } ";

        public static string UploadPkcs = @"{  ""key"": ""sdkalsdasdlasdlvasdasdbvlabdlv"",   ""password"": ""skdjskdj"" } ";



    }
}
