Imports System
Imports System.IO
Imports System.Net.Sockets
Imports System.Text.Encoding 'ASCII.GetString + ASCII.GetBytes
Imports System.Text.RegularExpressions
Imports System.Xml
Module main
    Dim host, port, channel, nickname, username, realname, owner, ownerfail, nsPass As String
    Public settingsFile As String = String.Format("{0}/settings.xml", Directory.GetCurrentDirectory())
    Dim client As TcpClient
    'Dim data As [Byte]()
    Dim ReadBuf As String = ""
    Dim CanRegex As Boolean = False
    Dim QuietStart As Boolean = False
    Dim nsUse As Boolean = False
    Dim gen As New Random
    Dim loggedIn As Boolean = False
    Dim firstPing As Boolean = False
    Dim nickSent As Boolean = False
    Dim userSent As Boolean = False
    Dim FirstRun As Boolean = True
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
                        checkString(out)
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
    Sub Pong(message As String)
        Dim pongMsg(1) As String
        pongMsg = message.Split(":")
        pongMsg(1) = pongMsg(1).TrimEnd(vbCr, vbLf)
        sendData("PONG " + pongMsg(1).ToString())
    End Sub
    Sub sendData(message As String)
        message = message + vbCrLf
        Dim stream As NetworkStream = client.GetStream()
        Dim data As Byte()
        data = New [Byte](65535) {}
        data = ASCII.GetBytes(message)
        stream.Write(data, 0, data.Length)
        Console.Write(">>> " + message)
    End Sub
    Sub checkString(message As String)
        Try
            'Check for PING messages
            cmdCheckPing(message)

            'Check if disconnected
            cmdCheckDisconnect(message)

            If loggedIn = True Then
                'Owner Only Functions
                cmdShutDown(message)
                cmdChangeNick(message)
                cmdChangeOwner(message)
                cmdJoinChan(message)
                cmdPartChan(message)
                cmdNickServ(message)

                'Other Functions
                cmdDiceRoll(message)
                cmdGetOwner(message)
                cmdGetDose(message)
            End If
        Catch ex As Exception
            Console.WriteLine("---Something went wrong (Sub checkString)---")
#If DEBUG Then
            Console.WriteLine(ex.ToString())
#End If
        End Try
    End Sub

    'Special server sends
    Sub joinChannel()
#If DEBUG Then
        Console.WriteLine("---Joining Channels---")
#End If
        sendData(String.Format("JOIN {0}", channel))
    End Sub
    Sub chanMessage(chan As String, message As String)
        sendData(String.Format("PRIVMSG {0} {1}", chan, message))
    End Sub
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

    'Random Stuff
    Function removeSpaces(message As String)
        message = message.Replace(" ", "")
        Return message
    End Function
    Function numberGen(min As Integer, max As Integer)
        Return gen.Next(min, max + 1).ToString()
    End Function

    'Brain Modules
    'Owner-Only Functions:
    Sub cmdShutDown(message As String)
        'Shut down if requested by owner
        If InStr(message.ToLower(), String.Format("Goodnight, {0}", nickname).ToLower()) Then
            If getNickname(message) = owner Then
                sendNotice(owner, String.Format("Goodnight, {0}", owner))
                sendData("QUIT")
                End
            Else
                chanMessage(getChannel(message), ownerfail)
            End If
        End If
    End Sub
    Sub cmdChangeNick(message As String)
        'Change nickname if requested by owner
        If InStr(message.ToLower(), String.Format("{0}: Changenick", nickname).ToLower()) Then
            If getNickname(message) = owner Then
                Dim nick As String
                Dim start As Integer
                start = Len(nickname) + Len(": Changenick ")
                nick = getMessage(message)
                nick = nick.Substring(start, Len(getMessage(message)) - start - 1)
                nick = removeSpaces(nick).ToString()
                sendData(String.Format("NICK {0}", nick))
                nickname = nick
            Else
                chanMessage(getChannel(message), ownerfail)
            End If
        End If
    End Sub
    Sub cmdChangeOwner(message As String)
        'Change owner if requested by owner
        If InStr(message.ToLower(), String.Format("{0}: Changeowner", nickname).ToLower()) Then
            If getNickname(message) = owner Then
                Dim newowner As String
                Dim start As Integer
                start = Len(nickname) + Len(": Changeowner ")
                newowner = getMessage(message)
                newowner = newowner.Substring(start, Len(getMessage(message)) - start - 1)
                owner = removeSpaces(newowner)
                sendNotice(owner, "You are my new owner!")
            Else
                chanMessage(getChannel(message), ownerfail)
            End If
        End If
    End Sub
    Sub cmdJoinChan(message As String)
        'Join channel if requested by owner
        If InStr(message.ToLower(), String.Format("{0}: Joinchan", nickname).ToLower()) Then
            If getNickname(message) = owner Then
                Dim newchan As String
                Dim start As Integer
                start = Len(nickname) + Len(": Joinchan ")
                newchan = getMessage(message)
                newchan = newchan.Substring(start, Len(getMessage(message)) - start - 1)
                newchan = removeSpaces(newchan)
                joinChan(newchan)
            Else
                chanMessage(getChannel(message), ownerfail)
            End If
        End If
    End Sub
    Sub cmdPartChan(message As String)
        'Part channel if requested by owner
        If InStr(message.ToLower(), String.Format("{0}: Partchan", nickname).ToLower()) Then
            If getNickname(message) = owner Then
                Dim oldchan As String
                Dim start As Integer
                start = Len(nickname) + Len(": Partchan")
                oldchan = getMessage(message)
                oldchan = oldchan.Substring(start, Len(getMessage(message)) - start - 1)
                oldchan = removeSpaces(oldchan)
                partChan(oldchan)
            Else
                chanMessage(getChannel(message), ownerfail)
            End If
        End If
    End Sub
    Sub cmdNickServ(message As String)
        'Identify with NickServ if requested by owner
        If InStr(message.ToLower(), String.Format("{0}: Identify", nickname).ToLower()) Then
            If getNickname(message) = owner Then
                sendNickServ()
            Else
                chanMessage(getChannel(message), ownerfail)
            End If
        End If
    End Sub
    'Other Functions:
    Sub cmdDiceRoll(message As String)
        If CanRegex Then
            If Regex.IsMatch(getMessage(message), "\d+d\d+", RegexOptions.IgnoreCase) Then
#If DEBUG Then
                Console.WriteLine("---Found Diceroll Match---")
#End If
                Dim rolls As Integer = Regex.Match(getMessage(message), "(?<rolls>\d+)d(?<max>\d+)", RegexOptions.IgnoreCase).Result("${rolls}")
                Dim max As Integer = Regex.Match(getMessage(message), "(?<rolls>\d+)d(?<max>\d+)", RegexOptions.IgnoreCase).Result("${max}")
                Dim resultArray(rolls - 1) As String
#If DEBUG Then
                Console.WriteLine("Checking number limits")
#End If
                If rolls > 100 Or max > 500 Then
#If DEBUG Then
                    Console.WriteLine(String.Format("{0} of {1} tried to roll {2} times with a max of {3} chances. Stopping roll.", getNickname(message), getChannel(message), rolls.ToString(), max.ToString()))
#End If
                    chanMessage(getChannel(message), String.Format("{0}: Sorry, but I can't work with numbers that large. I only support up to 100 rolls at a time, each roll generating numbers 1 - 500", getNickname(message)))
                    Exit Sub
                End If
                If rolls = 0 Or max = 0 Then
#If DEBUG Then
                    Console.WriteLine(String.Format("{0} of {1} tried to roll {2} times with a max of {3} chances. Stopping roll.", getNickname(message), getChannel(message), rolls.ToString(), max.ToString()))
#End If
                    chanMessage(getChannel(message), String.Format("{0}: Sorry, but I can't make a diceroll with 0 numbers or chances.", getNickname(message)))
                    Exit Sub
                End If
                If max = 1 Then
#If DEBUG Then
                    Console.WriteLine(String.Format("{0} of {1} tried to roll with a maximum number of 1. Stopping roll.", getNickname(message), getChannel(message)))
#End If
                    chanMessage(getChannel(message), String.Format("{0}: Sorry, but I can't generate a random whole number between 1 and 1.", getNickname(message)))
                    Exit Sub
                End If
#If DEBUG Then
                Console.WriteLine("---Starting For Loop---")
#End If
                For i As Integer = 1 To rolls
                    resultArray(i - 1) = numberGen(1, max)
#If DEBUG Then
                    Console.WriteLine(String.Format("---Generating number for roll {0}---", i.ToString()))
#End If
                Next
                chanMessage(getChannel(message), String.Format("Rolling {0} dice with {1} sides... Results: [{2}]", rolls, max, String.Join(", ", resultArray)))
            End If
        End If
    End Sub
    Sub cmdGetOwner(message As String)
        'Return owner info if requested
        If InStr(message.ToLower(), String.Format("{0}: Ownerinfo", nickname).ToLower()) Then
            chanMessage(getChannel(message), String.Format("{0} is my owner!", owner))
        End If
    End Sub
    Sub cmdGetDose(message As String)
        If CanRegex Then
            If Regex.IsMatch(getMessage(message), "!dose \d+ \d+", RegexOptions.IgnoreCase) Then
                Dim min As Integer = Regex.Match(getMessage(message), "!dose\ (?<min>\d+)\ (?<max>\d+)", RegexOptions.IgnoreCase).Result("${min}")
                Dim max As Integer = Regex.Match(getMessage(message), "!dose\ (?<min>\d+)\ (?<max>\d+)", RegexOptions.IgnoreCase).Result("${max}")
                chanMessage(getChannel(message), String.Format("{0}: You should take {1}mg!", getNickname(message), numberGen(min, max).ToString()))
            End If
        End If
    End Sub
    Sub cmdCheckDisconnect(message As String)
        If Len(message) > Len("ERROR :Closing Link:") Then
            If message.Substring(0, Len("ERROR :Closing Link:")) = "ERROR :Closing Link:" Then
                servConnect()
                runLoop()
            End If
        End If
    End Sub
    Sub cmdCheckPing(message As String)
        'Autorespond to PING messages
        If Len(message) > 6 Then
            If message.Substring(0, 6) = "PING :" Then
#If DEBUG Then
                Console.WriteLine("---Ping Found. Responding---")
#End If
                Pong(message)
                If firstPing = False Then firstPing = True
            End If
        End If
    End Sub
End Module
