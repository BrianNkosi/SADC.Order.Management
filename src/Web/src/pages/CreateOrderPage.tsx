import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCustomers } from '../api/customers';
import { useCreateOrder } from '../api/orders';
import type { CreateOrderLineItemRequest } from '../types';

const SADC_CURRENCIES = ['ZAR', 'BWP', 'MZN', 'ZMW', 'NAD', 'LSL', 'SZL', 'MWK', 'MGA', 'MUR', 'SCR', 'TZS', 'AOA', 'KMF', 'CDF', 'USD', 'ZWL'];

export function CreateOrderPage() {
  const navigate = useNavigate();
  const createOrder = useCreateOrder();
  const { data: customersData } = useCustomers('', 1, 100);

  const [customerId, setCustomerId] = useState('');
  const [currencyCode, setCurrencyCode] = useState('ZAR');
  const [lineItems, setLineItems] = useState<CreateOrderLineItemRequest[]>([
    { productSku: '', quantity: 1, unitPrice: 0 },
  ]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  function addLineItem() {
    setLineItems([...lineItems, { productSku: '', quantity: 1, unitPrice: 0 }]);
  }

  function removeLineItem(index: number) {
    setLineItems(lineItems.filter((_, i) => i !== index));
  }

  function updateLineItem(index: number, field: keyof CreateOrderLineItemRequest, value: string | number) {
    const updated = [...lineItems];
    updated[index] = { ...updated[index], [field]: value };
    setLineItems(updated);
  }

  function validate() {
    const errs: Record<string, string> = {};
    if (!customerId) errs.customerId = 'Customer is required';
    if (!currencyCode) errs.currencyCode = 'Currency is required';
    if (lineItems.length === 0) errs.lineItems = 'At least one line item is required';
    lineItems.forEach((li, i) => {
      if (!li.productSku.trim()) errs[`sku_${i}`] = 'SKU is required';
      if (li.quantity <= 0) errs[`qty_${i}`] = 'Quantity must be > 0';
      if (li.unitPrice < 0) errs[`price_${i}`] = 'Price must be ≥ 0';
    });
    return errs;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const errs = validate();
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    try {
      const order = await createOrder.mutateAsync({ customerId, currencyCode, lineItems });
      navigate(`/orders/${order.id}`);
    } catch {
      setErrors({ form: 'Failed to create order. Please try again.' });
    }
  }

  const total = lineItems.reduce((sum, li) => sum + li.quantity * li.unitPrice, 0);

  return (
    <main style={{ maxWidth: 700 }}>
      <h1>Create Order</h1>
      <form onSubmit={handleSubmit} noValidate>
        {errors.form && <p role="alert" className="error">{errors.form}</p>}

        <div style={fieldStyle}>
          <label htmlFor="customer">Customer *</label>
          <select
            id="customer"
            value={customerId}
            onChange={(e) => setCustomerId(e.target.value)}
            style={inputStyle}
            aria-invalid={!!errors.customerId}
          >
            <option value="">Select a customer</option>
            {customersData?.items.map((c) => (
              <option key={c.id} value={c.id}>{c.name} ({c.countryCode})</option>
            ))}
          </select>
          {errors.customerId && <p className="error">{errors.customerId}</p>}
        </div>

        <div style={fieldStyle}>
          <label htmlFor="currency">Currency *</label>
          <select
            id="currency"
            value={currencyCode}
            onChange={(e) => setCurrencyCode(e.target.value)}
            style={inputStyle}
            aria-invalid={!!errors.currencyCode}
          >
            {SADC_CURRENCIES.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>

        <fieldset style={{ border: '1px solid #ddd', padding: '1rem', marginBottom: '1rem' }}>
          <legend>Line Items</legend>
          {errors.lineItems && <p className="error">{errors.lineItems}</p>}

          {lineItems.map((li, i) => (
            <div key={i} style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem', alignItems: 'flex-end' }}>
              <div style={{ flex: 2 }}>
                <label htmlFor={`sku-${i}`}>SKU</label>
                <input
                  id={`sku-${i}`}
                  value={li.productSku}
                  onChange={(e) => updateLineItem(i, 'productSku', e.target.value)}
                  style={inputStyle}
                  aria-invalid={!!errors[`sku_${i}`]}
                />
              </div>
              <div style={{ flex: 1 }}>
                <label htmlFor={`qty-${i}`}>Qty</label>
                <input
                  id={`qty-${i}`}
                  type="number"
                  min={1}
                  value={li.quantity}
                  onChange={(e) => updateLineItem(i, 'quantity', parseInt(e.target.value) || 0)}
                  style={inputStyle}
                  aria-invalid={!!errors[`qty_${i}`]}
                />
              </div>
              <div style={{ flex: 1 }}>
                <label htmlFor={`price-${i}`}>Unit Price</label>
                <input
                  id={`price-${i}`}
                  type="number"
                  min={0}
                  step="0.01"
                  value={li.unitPrice}
                  onChange={(e) => updateLineItem(i, 'unitPrice', parseFloat(e.target.value) || 0)}
                  style={inputStyle}
                  aria-invalid={!!errors[`price_${i}`]}
                />
              </div>
              <button type="button" onClick={() => removeLineItem(i)} aria-label={`Remove item ${i + 1}`}>
                ✕
              </button>
            </div>
          ))}
          <button type="button" onClick={addLineItem} style={{ marginTop: '0.5rem' }}>
            + Add Line Item
          </button>
        </fieldset>

        <p style={{ fontWeight: 'bold', fontSize: '1.1rem' }}>
          Total: {currencyCode} {total.toFixed(2)}
        </p>

        <button type="submit" disabled={createOrder.isPending} className="btn btn-primary">
          {createOrder.isPending ? 'Creating...' : 'Create Order'}
        </button>
      </form>
    </main>
  );
}

const fieldStyle: React.CSSProperties = { marginBottom: '1rem' };
const inputStyle: React.CSSProperties = { width: '100%', padding: '0.5rem', display: 'block', marginTop: '0.25rem' };
