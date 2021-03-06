﻿[<AutoOpen>]
module FSnack.CoreLib.Aliases

let result = Result.computationBuilder
let maybe = Option.computationBuilder

type ILogger = Castle.Core.Logging.ILogger
type IKernel = Castle.MicroKernel.IKernel
type RefCancellationToken = {Token:System.Threading.CancellationToken}
type CancellationTokenSource = System.Threading.CancellationTokenSource