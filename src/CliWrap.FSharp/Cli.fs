module UnMango.CliWrap.FSharp.Cli

open System.Diagnostics
open System.Text
open System.Threading
open CliWrap
open CliWrap.Buffered
open CliWrap.Builders
open CliWrap.EventStream

/// <summary>
/// Creates a copy of this command, setting the arguments to the specified value.
/// </summary>
/// <param name="args">The string representation of the arguments.</param>
/// <param name="command">The Command object to add the arguments to.</param>
/// <returns>A new Command object with the specified arguments.</returns>
/// <remarks>
/// Avoid using this overload, as it requires the arguments to be escaped manually.
/// Formatting errors may lead to unexpected bugs and security vulnerabilities.
/// </remarks>
let arg (args: string) (command: Command) = command.WithArguments(args)

/// <summary>
/// Creates a copy of this command, setting the arguments to the value obtained by formatting the specified enumeration.
/// </summary>
/// <param name="args">The arguments to be added to the command.</param>
/// <param name="command">The command to add the arguments to.</param>
/// <returns>A new command object with the provided arguments.</returns>
let args (args: string seq) (command: Command) = command.WithArguments(args)

/// <summary>
/// Creates a copy of this command, setting the arguments to the value obtained by formatting the specified enumeration.
/// </summary>
/// <param name="args">The arguments to be added to the command.</param>
/// <param name="escape">The escape options to apply to the arguments.</param>
/// <param name="command">The original command.</param>
/// <returns>A new command object with the arguments and escape options added.</returns>
let argse args escape (command: Command) = command.WithArguments(args, escape)

/// <summary>
/// Creates a copy of this command, setting the arguments to the value configured by the specified delegate.
/// </summary>
/// <param name="f">The function that builds the arguments using an ArgumentsBuilder.</param>
/// <param name="command">The command for which the arguments are being built.</param>
/// <returns>A new command object with the arguments added.</returns>
let argsf (f: ArgumentsBuilder -> 'a) (command: Command) =
    command.WithArguments(fun b -> f b |> ignore)

/// <summary>
/// Creates a new command with the specified parameters.
/// </summary>
/// <param name="target">The target executable or script file.</param>
/// <param name="args">The arguments to be passed to the target.</param>
/// <param name="workDir">The working directory for the command.</param>
/// <param name="policy">The resource policy to use for the command.</param>
/// <param name="creds">The credentials to use for the command.</param>
/// <param name="env">The environment variables for the command.</param>
/// <param name="v">The validation for the command.</param>
/// <param name="stdin">The input pipe source for the command.</param>
/// <param name="stdout">The output pipe target for the command.</param>
/// <param name="stderr">The error pipe target for the command.</param>
/// <returns>A new Command instance.</returns>
/// <remarks>
/// v = verbose. Idk why you would use this, but it's there if you want to
/// </remarks>
let commandv target args workDir policy creds env v stdin stdout stderr =
    Command(target, args, workDir, policy, creds, env, v, stdin, stdout, stderr)

/// <summary>
/// Creates a copy of this command, setting the user credentials to the specified value.
/// </summary>
/// <param name="credentials">The credentials to be applied.</param>
/// <param name="command">The command to apply the credentials to.</param>
/// <returns>The modified command with the credentials applied.</returns>
let creds (credentials: Credentials) (command: Command) = command.WithCredentials(credentials)

/// <summary>
/// Creates a copy of this command, setting the user credentials to the specified value.
/// </summary>
/// <param name="f">The function that builds the credentials with a CredentialsBuilder.</param>
/// <param name="command">The command for which the credentials are being built.</param>
/// <returns>The modified command with the credentials applied.</returns>
let credsf (f: CredentialsBuilder -> 'a) (command: Command) =
    command.WithCredentials(fun b -> f b |> ignore)

/// <summary>
/// Creates a copy of this command, setting the resource policy to the specified value.
/// </summary>
/// <param name="policy">The resource policy to apply.</param>
/// <param name="command">The command to apply the resource policy to.</param>
/// <returns>A copy of the command with the resource policy applied.</returns>
let resourcePolicy (policy: ResourcePolicy) (command: Command) = command.WithResourcePolicy(policy)

/// <summary>
/// Creates a copy of this command, setting the resource policy to the value configured by the specified delegate.
/// </summary>
/// <param name="f">The function that builds the resource policy with a ResourcePolicyBuilder.</param>
/// <param name="command">The command to apply the resource policy to.</param>
/// <returns>A copy of the command with the resource policy applied.</returns>
let resourcePolicyf (f: ResourcePolicyBuilder -> 'a) (command: Command) =
    command.WithResourcePolicy(fun b -> f b |> ignore)

/// <summary>
/// Creates a copy of this command, setting the environment variables to the specified value.
/// </summary>
/// <param name="env">A sequence of tuples representing key-value pairs of environment variables.</param>
/// <param name="command">The command to modify.</param>
/// <returns>A new command with the updated environment variables.</returns>
let env (env: (string * string) seq) (command: Command) =
    command.WithEnvironmentVariables(readOnlyDict env)

/// <summary>
/// Creates a copy of this command, setting the environment variables to the value configured by the specified delegate.
/// </summary>
/// <param name="f">The function that builds environment variables using an EnvironmentVariablesBuilder.</param>
/// <param name="command">The command to which the environment variables should be applied.</param>
/// <returns>The modified command with the updated environment variables.</returns>
let envf (f: EnvironmentVariablesBuilder -> 'a) (command: Command) =
    command.WithEnvironmentVariables(fun b -> f b |> ignore)

/// <summary>
/// Executes the command asynchronously.
/// </summary>
/// <param name="command">The command to execute.</param>
/// <returns>An <see cref="Async{CommandResult}"/> representing the asynchronous operation.</returns>
let exec (command: Command) =
    command.ExecuteAsync() |> CommandTask.op_Implicit |> Async.AwaitTask

/// <summary>
/// Executes the command asynchronously, buffering standard output and standard error in memory.
/// </summary>
/// <param name="command">The command to execute.</param>
/// <returns>An <see cref="Async{BufferedCommandResult}"/> representing the asynchronous operation.</returns>
let execBuffered (command: Command) =
    command.ExecuteBufferedAsync() |> CommandTask.op_Implicit |> Async.AwaitTask

/// <summary>
/// Executes the command asynchronously, buffering standard output and standard error in memory
/// and decoding both with the specified encoding.
/// </summary>
/// <param name="encoding">The encoding used to decode standard output and standard error.</param>
/// <param name="command">The command to execute.</param>
/// <returns>An <see cref="Async{BufferedCommandResult}"/> representing the asynchronous operation.</returns>
let execBufferede (encoding: Encoding) (command: Command) =
    command.ExecuteBufferedAsync(encoding)
    |> CommandTask.op_Implicit
    |> Async.AwaitTask

/// <summary>
/// Executes the command as a pull-based event stream.
/// </summary>
/// <param name="cancellationToken">The cancellation token. Cancelling it forcefully terminates the process.</param>
/// <param name="command">The command to execute.</param>
/// <returns>The command's events, starting with <c>Started</c> and ending with <c>Exited</c>.</returns>
/// <remarks>
/// The command does not start until the sequence is enumerated.
/// Abandoning the enumeration also forcefully terminates the process.
/// Match the events with the <c>Started</c>, <c>StdOut</c>, <c>StdErr</c> and <c>Exited</c> active pattern.
/// </remarks>
let listen (cancellationToken: CancellationToken) (command: Command) = command.ListenAsync(cancellationToken)

/// <summary>
/// Executes the command as a pull-based event stream, decoding standard output and standard error with the specified encoding.
/// </summary>
/// <param name="encoding">The encoding used to decode standard output and standard error.</param>
/// <param name="cancellationToken">The cancellation token. Cancelling it forcefully terminates the process.</param>
/// <param name="command">The command to execute.</param>
/// <returns>The command's events, starting with <c>Started</c> and ending with <c>Exited</c>.</returns>
/// <remarks>
/// The command does not start until the sequence is enumerated.
/// Abandoning the enumeration also forcefully terminates the process.
/// </remarks>
let listene (encoding: Encoding) (cancellationToken: CancellationToken) (command: Command) =
    command.ListenAsync(encoding, cancellationToken)

/// <summary>
/// Executes the command as a pull-based event stream, with separate forceful and graceful cancellation.
/// </summary>
/// <param name="forceful">The cancellation token to forcefully exit.</param>
/// <param name="graceful">The cancellation token to gracefully exit.</param>
/// <param name="command">The command to execute.</param>
/// <returns>The command's events, starting with <c>Started</c> and ending with <c>Exited</c>.</returns>
/// <remarks>
/// The command does not start until the sequence is enumerated.
/// Abandoning the enumeration also forcefully terminates the process.
/// </remarks>
let listenf (forceful: CancellationToken) (graceful: CancellationToken) (command: Command) =
    command.ListenAsync(Encoding.Default, Encoding.Default, forceful, graceful)

/// <summary>
/// Executes the command as a push-based event stream.
/// </summary>
/// <param name="cancellationToken">The cancellation token. Cancelling it forcefully terminates the process.</param>
/// <param name="command">The command to execute.</param>
/// <returns>An observable of the command's events, starting with <c>Started</c> and ending with <c>Exited</c>.</returns>
/// <remarks>
/// The command starts once per subscription. Unsubscribing forcefully terminates the process.
/// Match the events with the <c>Started</c>, <c>StdOut</c>, <c>StdErr</c> and <c>Exited</c> active pattern.
/// </remarks>
let observe (cancellationToken: CancellationToken) (command: Command) = command.Observe(cancellationToken)

/// <summary>
/// Executes the command as a push-based event stream, decoding standard output and standard error with the specified encoding.
/// </summary>
/// <param name="encoding">The encoding used to decode standard output and standard error.</param>
/// <param name="cancellationToken">The cancellation token. Cancelling it forcefully terminates the process.</param>
/// <param name="command">The command to execute.</param>
/// <returns>An observable of the command's events, starting with <c>Started</c> and ending with <c>Exited</c>.</returns>
/// <remarks>
/// The command starts once per subscription. Unsubscribing forcefully terminates the process.
/// </remarks>
let observee (encoding: Encoding) (cancellationToken: CancellationToken) (command: Command) =
    command.Observe(encoding, cancellationToken)

/// <summary>
/// Executes the command as a push-based event stream, with separate forceful and graceful cancellation.
/// </summary>
/// <param name="forceful">The cancellation token to forcefully exit.</param>
/// <param name="graceful">The cancellation token to gracefully exit.</param>
/// <param name="command">The command to execute.</param>
/// <returns>An observable of the command's events, starting with <c>Started</c> and ending with <c>Exited</c>.</returns>
/// <remarks>
/// The command starts once per subscription. Unsubscribing forcefully terminates the process.
/// </remarks>
let observef (forceful: CancellationToken) (graceful: CancellationToken) (command: Command) =
    command.Observe(Encoding.Default, Encoding.Default, forceful, graceful)

/// <summary>
/// Creates a copy of this command, setting the standard input pipe to the specified source.
/// </summary>
/// <param name="pipe">The pipe to attach to the standard input.</param>
/// <param name="command">The command to attach the pipe to.</param>
/// <returns>A copy of the command with the standard input pipe attached.</returns>
let stdin pipe (command: Command) = command.WithStandardInputPipe(pipe)

/// <summary>
/// Creates a copy of this command, setting the standard output pipe to the specified target.
/// </summary>
/// <param name="pipe">The pipe to redirect the standard output to.</param>
/// <param name="command">The command to redirect the standard output from.</param>
/// <returns>A copy of the command with the standard output pipe attached.</returns>
let stdout pipe (command: Command) = command.WithStandardOutputPipe(pipe)

/// <summary>
/// Creates a copy of this command, setting the standard error pipe to the specified target.
/// </summary>
/// <param name="pipe">The pipe to attach.</param>
/// <param name="command">The command to attach the pipe to.</param>
/// <returns>The modified command with the standard error pipe attached.</returns>
let stderr pipe (command: Command) = command.WithStandardErrorPipe(pipe)

/// <summary>
/// Creates a copy of this command, setting the target file path to the specified value.
/// </summary>
/// <param name="target">The target file to set.</param>
/// <param name="command">The original command instance.</param>
/// <returns>A new Command instance with the updated target file.</returns>
let target target (command: Command) = command.WithTargetFile(target)

/// <summary>
/// Creates a copy of this command, setting the validation options to the specified value.
/// </summary>
/// <param name="validation">The validation options to apply.</param>
/// <param name="command">The command to validate.</param>
/// <returns>A copy of the command with the validation options applied.</returns>
let validation validation (command: Command) = command.WithValidation(validation)

/// <summary>
/// Creates a copy of this command, setting the working directory path to the specified value.
/// </summary>
/// <param name="dir">The directory to set as the working directory.</param>
/// <param name="command">The command to modify.</param>
/// <returns>
/// A new command object with the working directory set to the specified directory.
/// </returns>
let workDir dir (command: Command) = command.WithWorkingDirectory(dir)

/// <summary>
/// Creates a new command to execute the target specified by <paramref name="target"/>.
/// </summary>
/// <param name="target">The target to be wrapped.</param>
/// <returns>A command to execute <paramref name="target"/>.</returns>
let wrap target = Command(target)

module Task =
    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>A task representing the execution of the command.</returns>
    /// <remarks><see cref="CommandTask"/> can be <see cref="await"/>ed like a <see cref="Task"/>.</remarks>
    let exec cancellationToken (command: Command) = command.ExecuteAsync(cancellationToken)

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="forceful">The cancellation token to forcefully exit.</param>
    /// <param name="graceful">The cancellation token to gracefully exit.</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>A task representing the execution of the command.</returns>
    /// <remarks><see cref="CommandTask"/> can be <see cref="await"/>ed like a <see cref="Task"/>.</remarks>
    let execf (forceful: CancellationToken) (graceful: CancellationToken) (command: Command) =
        command.ExecuteAsync(forceful, graceful)

    /// <summary>
    /// Executes the command asynchronously, letting the caller configure the underlying process.
    /// </summary>
    /// <param name="configureStartInfo">Configures the <see cref="ProcessStartInfo"/> before the process starts. Pass <c>ignore</c> to skip.</param>
    /// <param name="configureProcess">Configures the <see cref="Process"/> before it starts. Pass <c>ignore</c> to skip.</param>
    /// <param name="forceful">The cancellation token to forcefully exit.</param>
    /// <param name="graceful">The cancellation token to gracefully exit.</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>A task representing the execution of the command.</returns>
    /// <remarks>
    /// Changes made to the process may break CliWrap's own handling, such as stream redirection.
    /// <see cref="CommandTask"/> can be <see cref="await"/>ed like a <see cref="Task"/>.
    /// </remarks>
    let execp
        (configureStartInfo: ProcessStartInfo -> unit)
        (configureProcess: Process -> unit)
        (forceful: CancellationToken)
        (graceful: CancellationToken)
        (command: Command)
        =
        command.ExecuteAsync(configureStartInfo, configureProcess, forceful, graceful)

    /// <summary>
    /// Executes the command asynchronously, buffering standard output and standard error in memory.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>A task representing the execution of the command.</returns>
    /// <remarks><see cref="CommandTask"/> can be <see cref="await"/>ed like a <see cref="Task"/>.</remarks>
    let execBuffered (cancellationToken: CancellationToken) (command: Command) =
        command.ExecuteBufferedAsync(cancellationToken)

    /// <summary>
    /// Executes the command asynchronously, buffering standard output and standard error in memory
    /// and decoding both with the specified encoding.
    /// </summary>
    /// <param name="encoding">The encoding used to decode standard output and standard error.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>A task representing the execution of the command.</returns>
    /// <remarks><see cref="CommandTask"/> can be <see cref="await"/>ed like a <see cref="Task"/>.</remarks>
    let execBufferede (encoding: Encoding) (cancellationToken: CancellationToken) (command: Command) =
        command.ExecuteBufferedAsync(encoding, cancellationToken)

    /// <summary>
    /// Executes the command asynchronously, buffering standard output and standard error in memory.
    /// </summary>
    /// <param name="stdoutEncoding">The encoding used to decode standard output.</param>
    /// <param name="stderrEncoding">The encoding used to decode standard error.</param>
    /// <param name="forceful">The cancellation token to forcefully exit.</param>
    /// <param name="graceful">The cancellation token to gracefully exit. Pass <see cref="CancellationToken.None"/> for none.</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>A task representing the execution of the command.</returns>
    /// <remarks><see cref="CommandTask"/> can be <see cref="await"/>ed like a <see cref="Task"/>.</remarks>
    let execBufferedf
        (stdoutEncoding: Encoding)
        (stderrEncoding: Encoding)
        (forceful: CancellationToken)
        (graceful: CancellationToken)
        (command: Command)
        =
        command.ExecuteBufferedAsync(stdoutEncoding, stderrEncoding, forceful, graceful)

let toString (command: Command) = command.ToString()
