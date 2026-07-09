import { describe, expect, it } from 'vitest';
import { reactive } from 'vue';

import { ApiError } from '../../src/api/http';
import { FORM_ERROR, useFormValidation } from '../../src/composables/useFormValidation';

describe('useFormValidation', () => {
  it('should collect the first error per field and report invalid form', () => {
    const values = reactive({
      email: '',
      password: '123'
    });

    const validation = useFormValidation(values, [
      (currentValues) => ({
        email: currentValues.email ? '' : 'Email là bắt buộc.',
        password: currentValues.password.length >= 6 ? '' : 'Mật khẩu quá ngắn.'
      }),
      () => ({
        email: 'Không được ghi đè lỗi đầu tiên.'
      })
    ]);

    const isValid = validation.validate();

    expect(isValid).toBe(false);
    expect(validation.errors.value).toEqual({
      email: 'Email là bắt buộc.',
      password: 'Mật khẩu quá ngắn.'
    });
    expect(validation.hasErrors.value).toBe(true);
  });

  it('should clear field and form errors', () => {
    const values = reactive({
      email: '',
      password: ''
    });
    const validation = useFormValidation(values, []);

    validation.setFieldError('email', 'Sai định dạng.');
    validation.setFormError('Có lỗi tổng quát.');
    validation.clearFieldError('email');
    validation.clearFormError();

    expect(validation.errors.value).toEqual({});
    expect(validation.formError.value).toBe('');

    validation.setFieldError('password', 'Bắt buộc.');
    validation.setFormError('Lỗi khác.');
    validation.clearErrors();

    expect(validation.errors.value).toEqual({});
    expect(validation.formError.value).toBe('');
  });

  it('should map ApiError to a field error when code map matches', () => {
    const values = reactive({
      email: '',
      password: ''
    });
    const validation = useFormValidation(values, []);

    validation.applyApiError(
      new ApiError('Email đã tồn tại.', 400, { code: 'duplicate_email', message: 'Email đã tồn tại.' }),
      { duplicate_email: 'email' }
    );

    expect(validation.errors.value.email).toBe('Email đã tồn tại.');
    expect(validation.formError.value).toBe('');
  });

  it('should map ApiError to form error when target is form or code is unknown', () => {
    const values = reactive({
      email: '',
      password: ''
    });
    const validation = useFormValidation(values, []);

    validation.applyApiError(
      new ApiError('Không thể đăng nhập.', 400, { code: 'invalid_credentials', message: 'Không thể đăng nhập.' }),
      { invalid_credentials: FORM_ERROR }
    );

    expect(validation.formError.value).toBe('Không thể đăng nhập.');

    validation.clearErrors();
    validation.applyApiError('unexpected', undefined, 'Thất bại.');
    expect(validation.formError.value).toBe('Thất bại.');
  });
});
