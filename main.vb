Imports System
Imports System.IO
Imports System.Net.Sockets
Imports System.Text.Encoding 'ASCII.GetString + ASCII.GetBytes
Imports System.Text.RegularExpressions
Imports System.Xml
Module main
    Public host, port, channel, nickname, username, realname, owner, ownerfail, nsPass As String
    Public settingsFile As String = String.Format("{0}/settings.xml", Directory.GetCurrentDirectory())
    Dim client As TcpClient
    'Dim data As [Byte]()
    Dim ReadBuf As String = ""
    Public CanRegex As Boolean = False
    Dim QuietStart As Boolean = False
    Dim nsUse As Boolean = False
    Dim gen As New Random
    Public loggedIn As Boolean = False
    Public firstPing As Boolean = False
    Public nickSent As Boolean = False
    Public userSent As Boolean = False
    Public FirstRun As Boolean = True
    Public diceMaxRolls, diceMaxSides As Integer
    Sub Main()
        checkStartFlags()
        XMLLoad()
        If QuietStart = False Then getParams()
        servConnect()
        runLoop()
    End Sub
    Sub XMLLoad()
        If Not File.Exists(settingsFile) Then
            WriteSampleConfig.Write()
        End If

        Dim xmlDoc = XDocument.Load(settingsFile)
        'Server Settings
        host = xmlDoc.<settings>.<server>.<host>.Value
        port = xmlDoc.<settings>.<server>.<port>.Value
        channel = xmlDoc.<settings>.<server>.<channel>.Value

        'Bot Settings
        nickname = xmlDoc.<settings>.<bot>.<nickname>.Value
        username = xmlDoc.<settings>.<bot>.<username>.Value
        realname = xmlDoc.<settings>.<bot>.<realname>.Value
        owner = xmlDoc.<settings>.<bot>.<owner>.Value

        'NickServ Settings
        If xmlDoc.<settings>.<nickserv>.<useNickServ>.Value = "True" Then nsUse = True
        nsPass = xmlDoc.<settings>.<nickserv>.<password>.Value

        'Diceroll Settings
        diceMaxRolls = xmlDoc.<settings>.<diceroll>.<diceMaxRolls>.Value
        diceMaxSides = xmlDoc.<settings>.<diceroll>.<diceMaxSides>.Value
        If diceMaxRolls > 75 Then
            Console.WriteLine("[WARN] settings.diceroll.diceMaxRolls is greater than 75. Defaulting to 75.")
            diceMaxRolls = 75
        End If
        If diceMaxSides = 1 Then
            Console.WriteLine("[WARN] settings.diceroll.diceMaxSides must be more than 1. Defaulting to 500.")
            diceMaxSides = 500
        End If

        'Miscellaneous Settings
        ownerfail = xmlDoc.<settings>.<misc>.<ownerfail>.Value
        If xmlDoc.<settings>.<misc>.<alwaysQuiet>.Value = True Then QuietStart = True
    End Sub
    Sub checkStartFlags()
        For Each s As String In My.Application.CommandLineArgs
            If s.ToLower = "/help" Then DisplayFlags()
            If s.ToLower = "/?" Then DisplayFlags()
            If s.ToLower = "/quiet" Then QuietStart = True
            If s.ToLower = "/gencfg" Then WriteSampleConfig.genNew()
        Next
    End Sub
    Sub DisplayFlags()
        Console.WriteLine("Welcome to xeon927's IRC Bot")
        Console.WriteLine("Commandline flags are to be prefixed with /")
        Console.WriteLine("Current Command line flags are:")
        Console.WriteLine("     HELP - Displays this message")
        Console.WriteLine("        ? - Displays this message")
        Console.WriteLine("    QUIET - Boots quietly (does not ask for configuration - ")
        Console.WriteLine("            boots with settings.xml values)")
        Console.WriteLine("   GENCFG - Regenerates settings.xml file. Doing this WILL")
        Console.WriteLine("            overwrite custom settings")
        End
    End Sub
    Sub getParams()
        'Get Server Address
        Console.Write(String.Format("Server [{0}]: ", host.ToString()))
        ReadBuf = Console.ReadLine()
        If ReadBuf = "" Then host = host Else host = ReadBuf

        'Get Server Port
        Console.Write(String.Format("Port [{0}]: ", port.ToString()))
        ReadBuf = Console.ReadLine()
        If ReadBuf = "" Then port = port Else port = ReadBuf

        'Get Channel
        Console.Write(String.Format("Channel [{0}]: ", channel.ToString()))
        ReadBuf = Console.ReadLine()
        If ReadBuf = "" Then channel = channel Else channel = ReadBuf

        'Get Nickname
        Console.Write(String.Format("Nickname [{0}]: ", nickname.ToString()))
        ReadBuf = Console.ReadLine()
        If ReadBuf = "" Then nickname = nickname Else nickname = ReadBuf

        'Get Username
        Console.Write(String.Format("Username [{0}]: ", username.ToString()))
        ReadBuf = Console.ReadLine()
        If ReadBuf = "" Then username = username Else username = ReadBuf

        'Get Realname
        Console.Write(String.Format("Realname [{0}]: ", realname.ToString()))
        ReadBuf = Console.ReadLine()
        If ReadBuf = "" Then realname = realname Else realname = ReadBuf

        'Get Bot Owner
        Console.Write(String.Format("Owner [{0}]: ", owner.ToString()))
        ReadBuf = Console.ReadLine()
        If ReadBuf = "" Then owner = owner Else owner = ReadBuf

        ReadBuf = String.Empty
    End Sub
    Sub servConnect()
        Try
            client = New TcpClient(host, port)
#If DEBUG Then
            Console.WriteLine("---SERVER CONNECTED---")
#End If
        Catch ex As Exception
#If DEBUG Then
            Console.WriteLine(ex.ToString() + vbCrLf)
#End If
        End Try
    End Sub
    Sub runLoop()
        Dim stream As NetworkStream = client.GetStream()
        Dim responseData As [String] = [String].Empty
        stream.ReadTimeout = 1000
        Do
            Dim data As Byte()
            data = New [Byte](0) {}
            Dim out As String = String.Empty
            Dim charIn As String = String.Empty
            Try
                Do
                    stream.Read(data, 0, 1)
                    charIn = ASCII.GetString(data)
                    If charIn = vbCr Then
                        out = out + vbCrLf
                        Console.Write("<<< " + out)
                        stream.Read(data, 0, 1)
                        chatStrings.check(out)
                        Exit Do
                    End If
                    out = out + charIn
                Loop

                If loggedIn = False Then doLogin()
            Catch ex As Exception
                'The following lines have been removed due to unnecessary spam. Sure, it might be needed, but probably not.
                'Every time the server doesn't send anything, the connection times out, and an error is thrown. Just about every 10 seconds.
                'Console.WriteLine("---No response from server---")
                'Console.WriteLine(ex.ToString())
            End Try
        Loop
    End Sub
    Sub doLogin()
        If FirstRun = True Then
            FirstRun = False
            Exit Sub
        End If
        If nickSent = False Then
            sendData(String.Format("NICK {0}", nickname.ToString()))
            nickSent = True
            Exit Sub
        End If
        If firstPing = True Then
            If userSent = False Then
                sendData(String.Format("USER {0} {1} {2} :{3}", username.ToString, "null", "null", realname.ToString()))
                userSent = True
                Exit Sub
            End If
            If nsUse = True Then
                sendNickServ()
            End If
            joinChannel()
            loggedIn = True
            CanRegex = True
        End If
    End Sub

    'Base Server Sends
    Sub sendData(message As String)
        message = message + vbCrLf
        Dim stream As NetworkStream = client.GetStream()
        Dim data As Byte()
        data = New [Byte](65535) {}
        data = ASCII.GetBytes(message)
        stream.Write(data, 0, data.Length)
        Console.Write(">>> " + message)
    End Sub
    Sub joinChannel()
#If DEBUG Then
        Console.WriteLine("---Joining Channels---")
#End If
        sendData(String.Format("JOIN {0}", channel))
    End Sub
    Sub chanMessage(chan As String, message As String)
        sendData(String.Format("PRIVMSG {0} {1}", chan, message))
    End Sub

    'Extended Server Sends
    Sub sendNotice(user As String, message As String)
        sendData(String.Format("NOTICE {0} {1}", user, message))
    End Sub
    Sub sendNickServ()
        sendData(String.Format("PRIVMSG NickServ IDENTIFY {0}", nsPass))
    End Sub
    Sub joinChan(chan As String)
        sendData(String.Format("JOIN {0}", chan))
    End Sub
    Sub partChan(chan As String)
        sendData(String.Format("PART {0}", chan))
    End Sub

    'Miscellaneous Functions
    Function removeSpaces(message As String)
        message = message.Replace(" ", "")
        Return message
    End Function
    Function numberGen(min As Integer, max As Integer)
        Return gen.Next(min, max + 1).ToString()
    End Function

    'Regex Stuff
    Function getNickname(message As String)
        If CanRegex Then
            Dim nick As String
            nick = msgRegex("nickname", message)
            Return nick
        End If
    End Function
    Function getUsername(message As String)
        If CanRegex Then
            Dim user As String
            user = msgRegex("username", message)
            Return user
        End If
    End Function
    Function getHostname(message As String)
        If CanRegex Then
            Dim host As String
            host = msgRegex("hostname", message)
            Return host
        End If
    End Function
    Function getChannel(message As String)
        If CanRegex Then
            Dim chan As String
            chan = msgRegex("channel", message)
            If chan.Substring(0, 1) = "#" Then Return chan Else Return getNickname(message)
            Return chan
        End If
    End Function
    Function getMessage(message As String)
        If CanRegex Then
            Dim msg As String
            msg = msgRegex("message", message)
            Return msg
        End If
    End Function
    Function msgRegex(output As String, input As String)
        If CanRegex Then
            Dim Pattern As String = "^:(?<nickname>.+?)!(?<username>.+?)@(?<hostname>.+?)\ PRIVMSG\ (?<channel>.+?)\ :(?<message>.+?)$"
            Dim r As New Regex(Pattern)
            Dim m As Match = r.Match(input)
            Dim nickname, username, hostname, channel, message As String
            If m.Success Then
                Try
                    nickname = r.Match(input).Result("${nickname}")
                    username = r.Match(input).Result("${username}")
                    hostname = r.Match(input).Result("${hostname}")
                    channel = r.Match(input).Result("${channel}")
                    message = r.Match(input).Result("${message}")
                Catch ex As Exception
                    Console.WriteLine("---Something went wrong (Sub msgRegex)---")
#If DEBUG Then
                    Console.WriteLine(ex.ToString())
#End If
                End Try
            Else
                Return "--Operation Failed--"
                Exit Function
            End If
            If output = "nickname" Then Return nickname
            If output = "username" Then Return username
            If output = "hostname" Then Return hostname
            If output = "channel" Then Return channel
            If output = "message" Then Return message
            Return "--Operation Failed--"
        Else
            Exit Function
        End If
    End Function
End Module
