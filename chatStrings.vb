Imports System.Text.RegularExpressions

Module chatStrings
    Sub check(message As String)
        Try
            'Check for PING messages
            cmdCheckPing(message)

            'Check if disconnected
            cmdCheckDisconnect(message)

            If main.loggedIn = True Then
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
        sendData("PONG " + pongMsg(1).ToString())
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
