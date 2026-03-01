import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCreateCustomer } from '../api/customers';

const SADC_COUNTRIES = [
  { code: 'AO', name: 'Angola' },
  { code: 'BW', name: 'Botswana' },
  { code: 'CD', name: 'DR Congo' },
  { code: 'KM', name: 'Comoros' },
  { code: 'SZ', name: 'Eswatini' },
  { code: 'LS', name: 'Lesotho' },
  { code: 'MG', name: 'Madagascar' },
  { code: 'MW', name: 'Malawi' },
  { code: 'MU', name: 'Mauritius' },
  { code: 'MZ', name: 'Mozambique' },
  { code: 'NA', name: 'Namibia' },
  { code: 'SC', name: 'Seychelles' },
  { code: 'ZA', name: 'South Africa' },
  { code: 'TZ', name: 'Tanzania' },
  { code: 'ZM', name: 'Zambia' },
  { code: 'ZW', name: 'Zimbabwe' },
];

export function CreateCustomerPage() {
  const navigate = useNavigate();
  const createCustomer = useCreateCustomer();
  const [form, setForm] = useState({ name: '', email: '', countryCode: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});

  function validate() {
    const errs: Record<string, string> = {};
    if (!form.name.trim()) errs.name = 'Name is required';
    if (!form.email.trim()) errs.email = 'Email is required';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) errs.email = 'Invalid email';
    if (!form.countryCode) errs.countryCode = 'Country is required';
    return errs;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const errs = validate();
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    try {
      await createCustomer.mutateAsync(form);
      navigate('/customers');
    } catch {
      setErrors({ form: 'Failed to create customer. Please try again.' });
    }
  }

  return (
    <main style={{ maxWidth: 500 }}>
      <h1>Create Customer</h1>
      <form onSubmit={handleSubmit} noValidate>
        {errors.form && <p role="alert" className="error">{errors.form}</p>}

        <div style={fieldStyle}>
          <label htmlFor="name">Name *</label>
          <input
            id="name"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            aria-invalid={!!errors.name}
            aria-describedby={errors.name ? 'name-error' : undefined}
            style={inputStyle}
          />
          {errors.name && <p id="name-error" className="error">{errors.name}</p>}
        </div>

        <div style={fieldStyle}>
          <label htmlFor="email">Email *</label>
          <input
            id="email"
            type="email"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            aria-invalid={!!errors.email}
            aria-describedby={errors.email ? 'email-error' : undefined}
            style={inputStyle}
          />
          {errors.email && <p id="email-error" className="error">{errors.email}</p>}
        </div>

        <div style={fieldStyle}>
          <label htmlFor="country">Country (SADC) *</label>
          <select
            id="country"
            value={form.countryCode}
            onChange={(e) => setForm({ ...form, countryCode: e.target.value })}
            aria-invalid={!!errors.countryCode}
            style={inputStyle}
          >
            <option value="">Select a country</option>
            {SADC_COUNTRIES.map((c) => (
              <option key={c.code} value={c.code}>{c.name} ({c.code})</option>
            ))}
          </select>
          {errors.countryCode && <p className="error">{errors.countryCode}</p>}
        </div>

        <button type="submit" disabled={createCustomer.isPending} className="btn btn-primary">
          {createCustomer.isPending ? 'Creating...' : 'Create Customer'}
        </button>
      </form>
    </main>
  );
}

const fieldStyle: React.CSSProperties = { marginBottom: '1rem' };
const inputStyle: React.CSSProperties = { width: '100%', padding: '0.5rem', display: 'block', marginTop: '0.25rem' };
