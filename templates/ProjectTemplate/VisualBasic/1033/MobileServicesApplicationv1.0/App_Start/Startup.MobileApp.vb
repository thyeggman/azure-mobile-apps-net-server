Imports System
Imports System.Collections.Generic
Imports System.Data.Entity
Imports System.Web.Http
Imports Microsoft.Azure.Mobile.Server.Config
Imports Owin

Partial Public Class Startup
    Public Sub ConfigureMobileApp(ByVal app As IAppBuilder)

        Dim config As New HttpConfiguration()

        Dim mobileConfig As New MobileAppConfiguration()
        mobileConfig _
            .UseDefaultConfiguration() _
            .ApplyTo(config)

        Database.SetInitializer(New $safeinitializerclassname$())

        app.UseMobileAppAuthentication(config)
        app.UseWebApi(config)

    End Sub
End Class

Public Class $safeinitializerclassname$
    Inherits CreateDatabaseIfNotExists(Of $safecontextclassname$)

    Protected Overrides Sub Seed(ByVal context As $safecontextclassname$)
        Dim todoItems As List(Of TodoItem) = New List(Of TodoItem) From
            {
                New TodoItem With {.Id = Guid.NewGuid().ToString(), .Text = "First item", .Complete = False},
                New TodoItem With {.Id = Guid.NewGuid().ToString(), .Text = "Second item", .Complete = False}
            }

        For Each todoItem As TodoItem In todoItems
            context.Set(Of TodoItem).Add(todoItem)
        Next

        MyBase.Seed(context)

    End Sub
End Class
