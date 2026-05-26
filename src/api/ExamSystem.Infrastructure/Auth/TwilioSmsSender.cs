using ExamSystem.Application.Common.Interfaces;
using ExamSystem.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ExamSystem.Infrastructure.Auth;

public class TwilioSmsSender(
    TwilioSmsSettings settings,
    ILogger<TwilioSmsSender> logger) : ISmsSender
{
    public async Task SendAsync(string to, string body, CancellationToken ct = default)
    {
        if (!settings.Enabled)
            throw new InvalidOperationException("Twilio 短信发送未启用，请先配置 TwilioSms:Enabled。");

        if (string.IsNullOrWhiteSpace(settings.AccountSid) || string.IsNullOrWhiteSpace(settings.AuthToken))
            throw new InvalidOperationException("Twilio AccountSid/AuthToken 未配置。请检查环境变量或 TwilioSms 配置。");

        if (string.IsNullOrWhiteSpace(settings.MessagingServiceSid) && string.IsNullOrWhiteSpace(settings.FromPhoneNumber))
            throw new InvalidOperationException("Twilio 必须至少配置 FromPhoneNumber 或 MessagingServiceSid 之一。");

        TwilioClient.Init(settings.AccountSid, settings.AuthToken);

        var options = new CreateMessageOptions(new PhoneNumber(to))
        {
            Body = body
        };

        if (!string.IsNullOrWhiteSpace(settings.MessagingServiceSid))
            options.MessagingServiceSid = settings.MessagingServiceSid;
        else
            options.From = new PhoneNumber(settings.FromPhoneNumber!);

        ct.ThrowIfCancellationRequested();
        var result = await MessageResource.CreateAsync(options);

        logger.LogInformation("Twilio 短信已发送 sid={Sid} to={To} status={Status}", result.Sid, to, result.Status);
    }
}