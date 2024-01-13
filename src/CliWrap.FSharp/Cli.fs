module UnMango.CliWrap.FSharp.Cli

open System.Collections.Generic
open CliWrap
open CliWrap.Builders

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
let argsf (f: ArgumentsBuilder -> unit) (command: Command) = command.WithArguments(f)

/// <summary>
/// Creates a new command with the specified parameters.
/// </summary>
/// <param name="target">The target executable or script file.</param>
/// <param name="args">The arguments to be passed to the target.</param>
/// <param name="workDir">The working directory for the command.</param>
/// <param name="creds">The credentials to use for the command.</param>
/// <param name="env">The environment variables for the command.</param>
/// <param name="v">The validation for the command.</param>
/// <param name="stdin">The input pipe source for the command.</param>
/// <param name="stdout">The output pipe target for the command.</param>
/// <param name="stderr">The error pipe target for the command.</param>
/// <returns>A new Command instance.</returns>
/// <remarks>
/// v = verbose. Idk why you would use this, but its there if you want to
/// </remarks>
let commandv target args workDir creds env v stdin stdout stderr =
    Command(target, args, workDir, creds, env, v, stdin, stdout, stderr)

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
let credsf (f: CredentialsBuilder -> unit) (command: Command) = command.WithCredentials(f)

/// <summary>
/// Creates a copy of this command, setting the environment variables to the specified value.
/// </summary>
/// <param name="env">A sequence of tuples representing key-value pairs of environment variables.</param>
/// <param name="command">The command to modify.</param>
/// <returns>A new command with the updated environment variables.</returns>
let env (env: (string * string) seq) (command: Command) =
    command.WithEnvironmentVariables((dict env).AsReadOnly())

/// <summary>
/// Creates a copy of this command, setting the environment variables to the value configured by the specified delegate.
/// </summary>
/// <param name="f">The function that builds environment variables using an EnvironmentVariablesBuilder.</param>
/// <param name="command">The command to which the environment variables should be applied.</param>
/// <returns>The modified command with the updated environment variables.</returns>
let envf (f: EnvironmentVariablesBuilder -> unit) (command: Command) = command.WithEnvironmentVariables(f)

/// <summary>
/// Executes the command asynchronously.
/// </summary>
/// <param name="command">The command to execute.</param>
/// <returns>An <see cref="Async{CommandResult}"/> representing the asynchronous operation.</returns>
let exec (command: Command) =
    command.ExecuteAsync() |> CommandTask.op_Implicit |> Async.AwaitTask

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
    let execf forceful graceful (command: Command) =
        command.ExecuteAsync(graceful, forceful)

let toString (command: Command) = command.ToString()
