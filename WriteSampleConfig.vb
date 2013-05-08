Imports System.IO
Imports System.Xml
Module WriteSampleConfig
    Sub Write()
        Dim defaultConfig As New XDocument(
            New XElement("settings",
                         New XElement("server",
                                      New XElement("host", "irc.example.net"),
                                      New XElement("port", "6667"),
                                      New XElement("channel", "#channel")),
                         New XElement("bot",
                                      New XElement("nickname", "IRCBot"),
                                      New XElement("username", "IRCBot"),
                                      New XElement("realname", "xeon927's IRC Bot"),
                                      New XElement("owner", "botOwner")),
                         New XElement("nickserv",
                                      New XElement("useNickServ", "False"),
                                      New XElement("password", "password")),
                         New XElement("misc",
                                      New XElement("ownerfail", "Sorry, only my owner can make me do that."),
                                      New XElement("alwaysQuiet", "False")),
                         New XElement("diceroll",
                                      New XElement("diceMaxRolls", "75"),
                                      New XElement("diceMaxSides", "500"))))
        defaultConfig.Save(main.settingsFile)
    End Sub
    Sub genNew()
        If File.Exists(main.settingsFile) Then File.Delete(main.settingsFile)
        Write()
        End
    End Sub
End Module
