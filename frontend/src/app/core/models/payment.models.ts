export interface CreateIntentRequest {
  returnUrl: string;
}

export interface CreateIntentResponse {
  clientSecret?: string;   // Stripe
  approvalUrl?:  string;   // PayPal
  externalId:    string;
}
