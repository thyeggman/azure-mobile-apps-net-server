Imports System.Web.Http
Imports System.Web.Http.Tracing
Imports Microsoft.Azure.Mobile.Server
Imports Microsoft.Azure.Mobile.Server.Config

' Use the MobileAppController attribute for each ApiController you want to use  
' from your mobile clients 
<MobileAppController()>
Public Class ValuesController
    Inherits ApiController

    ' GET api/values
    Public Function GetValue() As String
        Dim settings As MobileAppSettingsDictionary = Me.Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings()

        Dim traceWriter As ITraceWriter = Me.Configuration.Services.GetTraceWriter()
        traceWriter.Info("Hello from " + settings.Name)

        Return "Hello World!"
    End Function

    ' POST api/values
    Public Function PostValue() As String
        Return "Hello World!"
    End Function
End Class