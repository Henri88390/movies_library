import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  loginForm!: FormGroup;
  isLoading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.initializeForm();
  }

  private initializeForm(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: [
        '',
        [Validators.required, Validators.minLength(6), this.passwordValidator],
      ],
    });
  }

  private passwordValidator(control: any): { [key: string]: any } | null {
    const value = control.value;
    if (!value) {
      return null;
    }

    const hasDigit = /[0-9]/.test(value);
    const hasLowercase = /[a-z]/.test(value);
    const isValidLength = value.length >= 6;

    const passwordValid = hasDigit && hasLowercase && isValidLength;

    if (!passwordValid) {
      return {
        passwordRequirements: {
          hasDigit,
          hasLowercase,
          isValidLength,
        },
      };
    }

    return null;
  }

  get passwordErrors() {
    const passwordControl = this.loginForm.get('password');
    if (passwordControl?.errors?.['passwordRequirements']) {
      return passwordControl.errors['passwordRequirements'];
    }
    return null;
  }

  onSubmit(): void {
    if (this.loginForm.valid && !this.isLoading) {
      this.isLoading = true;
      this.errorMessage = '';

      const loginData = this.loginForm.value;

      this.authService.login(loginData).subscribe({
        next: (response) => {
          console.log('Login successful:', response);
          this.router.navigate(['/movies']);
        },
        error: (error) => {
          console.error('Login failed:', error);
          this.errorMessage =
            error.error?.message || 'Login failed. Please try again.';
          this.isLoading = false;
        },
        complete: () => {
          this.isLoading = false;
        },
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.loginForm.controls).forEach((key) => {
      const control = this.loginForm.get(key);
      if (control) {
        control.markAsTouched();
      }
    });
  }

  getFieldError(fieldName: string): string | null {
    const field = this.loginForm.get(fieldName);
    if (field && field.touched && field.errors) {
      if (field.errors['required']) {
        return `${
          fieldName.charAt(0).toUpperCase() + fieldName.slice(1)
        } is required`;
      }
      if (field.errors['email']) {
        return 'Please enter a valid email address';
      }
      if (field.errors['minlength']) {
        return `Password must be at least ${field.errors['minlength'].requiredLength} characters`;
      }
      if (field.errors['passwordRequirements']) {
        return 'Password does not meet requirements';
      }
    }
    return null;
  }
}
