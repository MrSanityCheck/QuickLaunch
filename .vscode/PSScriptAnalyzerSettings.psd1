@{
    Severity = @('Error', 'Warning', 'Information')

    ExcludeRules = @(
        'PSAvoidUsingWriteHost'
    )

    Rules = @{
        PSAlignAssignmentStatement = @{
            Enable       = $true
            CheckHashtable = $true
        }
        PSAvoidLongLines = @{
            Enable            = $true
            MaximumLineLength = 120
        }
        PSPlaceCloseBrace = @{
            Enable              = $true
            NoEmptyLineBefore   = $false
            IgnoreOneLineBlock  = $true
            NewLineAfter        = $false
        }
        PSPlaceOpenBrace = @{
            Enable             = $true
            OnSameLine         = $true
            NewLineAfter       = $true
            IgnoreOneLineBlock = $true
        }
        PSProvideCommentHelp = @{
            Enable = $false
        }
        PSUseConsistentIndentation = @{
            Enable             = $true
            IndentationSize    = 4
            PipelineIndentation = 'IncreaseIndentationForFirstPipeline'
            Kind               = 'space'
        }
        PSUseConsistentWhitespace = @{
            Enable                         = $true
            CheckInnerBrace                = $true
            CheckOpenBrace                 = $true
            CheckOpenParen                 = $true
            CheckOperator                  = $true
            CheckPipe                      = $true
            CheckPipeForRedundantWhitespace = $true
            CheckSeparator                 = $true
            CheckParameter                 = $false
        }
    }
}
