﻿using System;
using NUnit.Framework;
using Braintree;
using Braintree.Exceptions;

namespace Braintree.Tests
{
    [TestFixture]
    class CreditCardTest
    {
        private BraintreeGateway gateway;

        [SetUp]
        public void Setup()
        {
            gateway = new BraintreeGateway
            {
                Environment = Environment.DEVELOPMENT,
                MerchantId = "integration_merchant_id",
                PublicKey = "integration_public_key",
                PrivateKey = "integration_private_key"
            };
        }

        [Test]
        public void TransparentRedirectURLForCreate_ReturnsCorrectValue()
        {
            Assert.AreEqual(Configuration.BaseMerchantURL() + "/payment_methods/all/create_via_transparent_redirect_request",
                    gateway.CreditCard.TransparentRedirectURLForCreate());
        }

        [Test]
        public void TransparentRedirectURLForUpdate_ReturnsCorrectValue()
        {
            Assert.AreEqual(Configuration.BaseMerchantURL() + "/payment_methods/all/update_via_transparent_redirect_request",
                    gateway.CreditCard.TransparentRedirectURLForUpdate());
        }

        [Test]
        public void TrData_ReturnsValidTrDataHash()
        {
            String trData = gateway.TrData(new CreditCardRequest(), "http://example.com");
            Assert.IsTrue(TrUtil.IsTrDataValid(trData));
        }


        [Test]
        public void Create_CreatesCreditCardForGivenCustomerId()
        {
            String id = Guid.NewGuid().ToString();
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;

            var creditCardRequest = new CreditCardRequest
            {
                CustomerId = customer.Id,
                Number = "5105105105105100",
                ExpirationDate = "05/12",
                CVV = "123",
                CardholderName = "Michael Angelo"
            };

            CreditCard creditCard = gateway.CreditCard.Create(creditCardRequest).Target;

            Assert.AreEqual("510510", creditCard.Bin);
            Assert.AreEqual("5100", creditCard.LastFour);
            Assert.AreEqual("05", creditCard.ExpirationMonth);
            Assert.AreEqual("2012", creditCard.ExpirationYear);
            Assert.AreEqual("Michael Angelo", creditCard.CardholderName);
            Assert.AreEqual(DateTime.Now.Year, creditCard.CreatedAt.Value.Year);
            Assert.AreEqual(DateTime.Now.Year, creditCard.UpdatedAt.Value.Year);
        }

        [Test]
        public void ConfirmTransparentRedirectCreate_CreatesTheCreditCard()
        {
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;

            CreditCardRequest trParams = new CreditCardRequest { CustomerId = customer.Id };

            CreditCardRequest request = new CreditCardRequest
            {
                CardholderName = "John Doe",
                Number = "5105105105105100",
                ExpirationDate = "05/12"
            };

            String queryString = TestHelper.QueryStringForTR(trParams, request, gateway.CreditCard.TransparentRedirectURLForCreate());
            Result<CreditCard> result = gateway.CreditCard.ConfirmTransparentRedirect(queryString);
            Assert.IsTrue(result.IsSuccess());
            CreditCard card = result.Target;
            Assert.AreEqual("John Doe", card.CardholderName);
            Assert.AreEqual("510510", card.Bin);
            Assert.AreEqual("05", card.ExpirationMonth);
            Assert.AreEqual("2012", card.ExpirationYear);
            Assert.AreEqual("05/2012", card.ExpirationDate);
            Assert.AreEqual("5100", card.LastFour);
            Assert.IsTrue(card.Token != null);
        }

        [Test]
        public void Find_FindsCreditCardByToken()
        {
            String id = Guid.NewGuid().ToString();
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;

            var creditCardRequest = new CreditCardRequest
            {
                CustomerId = customer.Id,
                Number = "5105105105105100",
                ExpirationDate = "05/12",
                CVV = "123",
                CardholderName = "Michael Angelo"
            };

            CreditCard originalCreditCard = gateway.CreditCard.Create(creditCardRequest).Target;
            CreditCard creditCard = gateway.CreditCard.Find(originalCreditCard.Token);

            Assert.AreEqual("510510", creditCard.Bin);
            Assert.AreEqual("5100", creditCard.LastFour);
            Assert.AreEqual("05", creditCard.ExpirationMonth);
            Assert.AreEqual("2012", creditCard.ExpirationYear);
            Assert.AreEqual("Michael Angelo", creditCard.CardholderName);
            Assert.AreEqual(DateTime.Now.Year, creditCard.CreatedAt.Value.Year);
            Assert.AreEqual(DateTime.Now.Year, creditCard.UpdatedAt.Value.Year);
        }

        [Test]
        public void Update_UpdatesCreditCardByToken()
        {
            String id = Guid.NewGuid().ToString();
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;

            var creditCardCreateRequest = new CreditCardRequest
            {
                CustomerId = customer.Id,
                Number = "5105105105105100",
                ExpirationDate = "05/12",
                CVV = "123",
                CardholderName = "Michael Angelo"
            };

            CreditCard originalCreditCard = gateway.CreditCard.Create(creditCardCreateRequest).Target;

            var creditCardUpdateRequest = new CreditCardRequest
            {
                CustomerId = customer.Id,
                Number = "4111111111111111",
                ExpirationDate = "12/05",
                CVV = "321",
                CardholderName = "Dave Inchy"
            };

            CreditCard creditCard = gateway.CreditCard.Update(originalCreditCard.Token, creditCardUpdateRequest).Target;

            Assert.AreEqual("411111", creditCard.Bin);
            Assert.AreEqual("1111", creditCard.LastFour);
            Assert.AreEqual("12", creditCard.ExpirationMonth);
            Assert.AreEqual("2005", creditCard.ExpirationYear);
            Assert.AreEqual("Dave Inchy", creditCard.CardholderName);
            Assert.AreEqual(DateTime.Now.Year, creditCard.CreatedAt.Value.Year);
            Assert.AreEqual(DateTime.Now.Year, creditCard.UpdatedAt.Value.Year);
        }

        [Test]
        public void UpdateViaTransparentRedirect()
        {
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;
            CreditCardRequest createRequest = new CreditCardRequest
            {
                CustomerId = customer.Id,
                CardholderName = "John Doe",
                Number = "5105105105105100",
                ExpirationDate = "05/12",
                BillingAddress = new AddressRequest
                {
                    PostalCode = "44444"
                }
            };
            CreditCard createdCard = gateway.CreditCard.Create(createRequest).Target;

            CreditCardRequest trParams = new CreditCardRequest
            {
                PaymentMethodToken = createdCard.Token
            };

            CreditCardRequest request = new CreditCardRequest
            {
                CardholderName = "Joe Cool"
            };

            String queryString = TestHelper.QueryStringForTR(trParams, request, gateway.CreditCard.TransparentRedirectURLForUpdate());
            Result<CreditCard> result = gateway.CreditCard.ConfirmTransparentRedirect(queryString);
            Assert.IsTrue(result.IsSuccess());
            CreditCard card = result.Target;
            Assert.AreEqual("Joe Cool", card.CardholderName);
            Assert.AreEqual("44444", card.BillingAddress.PostalCode);
        }

        [Test]
        public void Delete_DeletesTheCreditCard()
        {
            String id = Guid.NewGuid().ToString();
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;

            var creditCardRequest = new CreditCardRequest
            {
                CustomerId = customer.Id,
                Number = "5105105105105100",
                ExpirationDate = "05/12",
                CVV = "123",
                CardholderName = "Michael Angelo"
            };

            CreditCard creditCard = gateway.CreditCard.Create(creditCardRequest).Target;

            Assert.AreEqual(creditCard.Token, gateway.CreditCard.Find(creditCard.Token).Token);
            gateway.CreditCard.Delete(creditCard.Token);
            Assert.Throws<NotFoundException>(() => gateway.CreditCard.Find(creditCard.Token));
        }

        [Test]
        public void verifyValidCreditCard()
        {
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;
            CreditCardRequest request = new CreditCardRequest
            {
                CustomerId = customer.Id,
                CardholderName = "John Doe",
                CVV = "123",
                Number = "4111111111111111",
                ExpirationDate = "05/12",
                Options = new CreditCardOptionsRequest
                {
                    VerifyCard = true
                }
            };

            Result<CreditCard> result = gateway.CreditCard.Create(request);
            Assert.IsTrue(result.IsSuccess());
        }

        [Test]
        public void verifyInvalidCreditCard()
        {
            Customer customer = gateway.Customer.Create(new CustomerRequest()).Target;
            CreditCardRequest request = new CreditCardRequest
            {
                CustomerId = customer.Id,
                CardholderName = "John Doe",
                CVV = "123",
                Number = "5105105105105100",
                ExpirationDate = "05/12",
                Options = new CreditCardOptionsRequest
                {
                    VerifyCard = true
                }
            };

            Result<CreditCard> result = gateway.CreditCard.Create(request);
            Assert.IsFalse(result.IsSuccess());
            CreditCardVerification verification = result.CreditCardVerification;
            Assert.AreEqual("processor_declined", verification.Status);
        }
    }
}
