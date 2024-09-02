using Ecommerce.Models;
using Microsoft.Extensions.Options;
using Stripe;

public class PaymentService
{
    private readonly string _apiKey;

    public PaymentService(IOptions<StripeSettings> stripeSettings)
    {
        _apiKey = stripeSettings.Value.SecretKey;
        StripeConfiguration.ApiKey = _apiKey;
    }

    public Task<string> CreatePaymentIntent(decimal amount)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100), // Amount in cents
            Currency = "usd",
            PaymentMethodTypes = new List<string> { "card" },
        };

        var service = new PaymentIntentService();
        var paymentIntent = service.Create(options);

        return Task.FromResult(paymentIntent.ClientSecret);
    }
}
