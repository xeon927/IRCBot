Imports System.Text.RegularExpressions

Module chatStrings
    Sub check(message As String)
        Try
            'Check for PING messages
            cmdCheckPing(message)

            'Check if disconnected
            cmdCheckDisconnect(message)

            If main.loggedIn = True Then
                cmdCheckHighlight(message)
                'Owner Only Functions
                cmdShutDown(message)

                'Other Functions
                cmdDiceRoll(message)
                cmdGetDose(message)
                cmdHug(message)
            End If
        Catch ex As Exception
            Console.WriteLine("---Something went wrong (Sub checkString)---")
#If DEBUG Then
            Console.WriteLine(ex.ToString())
#End If
        End Try
    End Sub

    'Essential Stuff
    Sub cmdCheckDisconnect(message As String)
        If Len(message) > Len("ERROR :Closing Link:") Then
            If message.Substring(0, Len("ERROR :Closing Link:")) = "ERROR :Closing Link:" Then
                CanRegex = False
                loggedIn = False
                firstPing = False
                nickSent = False
                userSent = False
                FirstRun = False
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
    Sub Pong(message As String)
        Dim pongMsg(1) As String
        pongMsg = message.Split(":")
        pongMsg(1) = pongMsg(1).TrimEnd(vbCr, vbLf)
        sendData("PONG :" + pongMsg(1).ToString())
    End Sub

    'Highlight Handling
    Sub cmdCheckHighlight(message As String)
        If Regex.IsMatch(getMessage(message), "\w+: \w+ [A-Za-z0-9#]+", RegexOptions.IgnoreCase) Then
#If DEBUG Then
            Console.WriteLine("---Got match for (nickname: argument instruction)---")
#End If
            Dim fromNick, fromChan, nick, inst, arg As String
            Dim pattern As String = "(?<nickname>\w+): (?<instruction>\w+) (?<argument>[A-Za-z0-9#]+)"
            fromNick = getNickname(message)
            fromChan = getChannel(message)
            nick = Regex.Match(getMessage(message), pattern, RegexOptions.IgnoreCase).Result("${nickname}")
            inst = Regex.Match(getMessage(message), pattern, RegexOptions.IgnoreCase).Result("${instruction}")
            arg = Regex.Match(getMessage(message), pattern, RegexOptions.IgnoreCase).Result("${argument}")
            cmdRunHighlight(fromNick, fromChan, inst.ToLower(), arg)
            Exit Sub
        End If
        If Regex.IsMatch(getMessage(message), "\w+: \w+", RegexOptions.IgnoreCase) Then
#If DEBUG Then
            Console.WriteLine("---Got match for (nickname: argument)---")
#End If
            Dim fromNick, fromChan, nick, inst As String
            Dim pattern As String = "(?<nickname>\w+): (?<instruction>\w+)"
            fromNick = getNickname(message)
            fromChan = getChannel(message)
            nick = Regex.Match(getMessage(message), pattern, RegexOptions.IgnoreCase).Result("${nickname}")
            inst = Regex.Match(getMessage(message), pattern, RegexOptions.IgnoreCase).Result("${instruction}")
            cmdRunHighlight(fromNick, fromChan, inst.ToLower())
            Exit Sub
        End If
    End Sub
    Sub cmdRunHighlight(fromNick As String, fromChan As String, instruction As String)
        'Instructions without arguments
        Select Case instruction
            Case "ownerinfo" : cmdGetOwner(fromNick, fromChan)
            Case "version" : cmdVersion(fromNick, fromChan)
            Case "identify" : cmdNickServ(fromNick, fromChan)
            Case "help" : cmdHelp(fromNick, fromChan)
        End Select
    End Sub
    Sub cmdRunHighlight(fromNick As String, fromChan As String, instruction As String, arguments As String)
        'Instructions with arguments
        Select Case instruction
            Case "getvar" : cmdGetVar(fromNick, fromChan, arguments)
            Case "changenick" : cmdChangeNick(fromNick, fromChan, arguments)
            Case "changeowner" : cmdChangeOwner(fromNick, fromChan, arguments)
            Case "joinchan" : cmdJoinChan(fromNick, fromChan, arguments)
            Case "partchan" : cmdPartChan(fromNick, fromChan, arguments)
            Case "help" : cmdHelp(fromNick, fromChan, arguments)
        End Select
    End Sub

    'Highlight Modules
    Sub cmdGetOwner(fromNick As String, fromChan As String)
        'Return owner info if requested
        chanMessage(fromChan, String.Format("{0}: {1} is my owner!", fromNick, owner))
    End Sub
    Sub cmdVersion(fromNick As String, fromChan As String)
        chanMessage(fromChan, String.Format("{0}: I am running version {1} of xeon927's IRC Bot.", fromNick, version))
    End Sub
    Sub cmdHelp(fromNick As String, fromChan As String, arguments As String)
        help.getHelp(fromNick, fromChan, arguments)
    End Sub
    Sub cmdHelp(fromNick As String, fromChan As String)
        help.showHelp(fromNick, fromChan)
    End Sub

    'Owner-Only
    Sub cmdNickServ(fromNick As String, fromChan As String)
        If fromNick = owner Then
            sendNickServ()
        Else
            chanMessage(fromChan, String.Format("{0}: {1}", fromNick, ownerfail))
        End If
    End Sub
    Sub cmdGetVar(fromNick As String, fromChan As String, arguments As String)
        If fromNick = owner Then
            If InStr(arguments, "settingsFile") Then chanMessage(fromChan, settingsFile)
            If InStr(arguments, "diceMaxRolls") Then chanMessage(fromChan, diceMaxRolls)
            If InStr(arguments, "diceMaxSides") Then chanMessage(fromChan, diceMaxSides)
        Else
            chanMessage(fromChan, String.Format("{0}: {1}", fromNick, ownerfail))
        End If
    End Sub
    Sub cmdChangeNick(fromNick As String, fromChan As String, arguments As String)
        If fromNick = owner Then
            arguments = removeSpaces(arguments)
            sendData(String.Format("NICK {0}", arguments))
            nickname = arguments
        Else
            chanMessage(fromChan, String.Format("{0}: {1}", fromNick, ownerfail))
        End If
    End Sub
    Sub cmdChangeOwner(fromNick As String, fromChan As String, arguments As String)
        If fromNick = owner Then
            owner = removeSpaces(arguments)
            sendNotice(owner, "You are my new owner!")
        Else
            chanMessage(fromChan, String.Format("{0}: {1}", fromNick, ownerfail))
        End If
    End Sub
    Sub cmdJoinChan(fromNick As String, fromChan As String, arguments As String)
        If fromNick = owner Then
            joinChan(arguments)
        Else
            chanMessage(fromChan, String.Format("{0}: {1}", fromNick, ownerfail))
        End If
    End Sub
    Sub cmdPartChan(fromNick As String, fromChan As String, arguments As String)
        If fromNick = owner Then
            partChan(arguments)
        Else
            chanMessage(fromChan, String.Format("{0}: {1}", fromNick, ownerfail))
        End If
    End Sub


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

    'Other Functions:
    Sub cmdDiceRoll(message As String)
        If CanRegex Then
            If Regex.IsMatch(getMessage(message), "!\d+d\d+", RegexOptions.IgnoreCase) Then
#If DEBUG Then
                Console.WriteLine("---Found Diceroll Match---")
#End If
                Dim rolls As Integer = Regex.Match(getMessage(message), "!(?<rolls>\d+)d(?<max>\d+)", RegexOptions.IgnoreCase).Result("${rolls}")
                Dim max As Integer = Regex.Match(getMessage(message), "!(?<rolls>\d+)d(?<max>\d+)", RegexOptions.IgnoreCase).Result("${max}")
                Dim resultArray(rolls - 1) As String
#If DEBUG Then
                Console.WriteLine("Checking number limits")
#End If
                If rolls > diceMaxRolls Or max > diceMaxSides Then
#If DEBUG Then
                    Console.WriteLine(String.Format("{0} of {1} tried to roll {2} times with a max of {3} chances. Stopping roll.", getNickname(message), getChannel(message), rolls.ToString(), max.ToString()))
#End If
                    chanMessage(getChannel(message), String.Format("{0}: Sorry, but I can't work with numbers that large. I only support up to {1} rolls at a time, each roll generating numbers 1 - {2}", getNickname(message), diceMaxRolls, diceMaxSides))
                    Exit Sub
                End If
                If rolls = 0 Or max = 0 Then
#If DEBUG Then
                    Console.WriteLine(String.Format("{0} of {1} tried to roll {2} times with a max of {3} chances. Stopping roll.", getNickname(message), getChannel(message), rolls.ToString(), max.ToString()))
#End If
                    chanMessage(getChannel(message), String.Format("{0}: Sorry, but I can't make a diceroll with 0 rolls or chances.", getNickname(message)))
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
    Sub cmdGetDose(message As String)
        If CanRegex Then
            If Regex.IsMatch(getMessage(message), "!dose \d+ \d+", RegexOptions.IgnoreCase) Then
                Dim min As Integer = Regex.Match(getMessage(message), "!dose\ (?<min>\d+)\ (?<max>\d+)", RegexOptions.IgnoreCase).Result("${min}")
                Dim max As Integer = Regex.Match(getMessage(message), "!dose\ (?<min>\d+)\ (?<max>\d+)", RegexOptions.IgnoreCase).Result("${max}")
                chanMessage(getChannel(message), String.Format("{0}: You should take {1}mg!", getNickname(message), numberGen(min, max).ToString()))
            End If
        End If
    End Sub
    Sub cmdHug(message As String)
        If InStr(message, String.Format("hugs {0}", nickname)) Then
            If getNickname(message) = owner Then
                Select Case numberGen(0, 3)
                    Case 1
                        chanMessage(getChannel(message), String.Format("{1}ACTION snuggles up to {0}{1}", getNickname(message), Chr(&H1)))
                    Case 2
                        chanMessage(getChannel(message), String.Format("{1}ACTION cuddles {0}{1}", getNickname(message), Chr(&H1)))
                    Case 3
                        chanMessage(getChannel(message), String.Format("{1}ACTION hugs {0} in return{1}", getNickname(message), Chr(&H1)))
                    Case Else
                        chanMessage(getChannel(message), String.Format("{1}ACTION hugs {0}{1}", getNickname(message), Chr(&H1)))
                        sendNotice(owner, "Something went wrong with cmdHug... :(")
                End Select
            Else
                chanMessage(getChannel(message), String.Format("{0}: Aww... :)", getNickname(message)))
            End If
        End If
    End Sub
End Module
