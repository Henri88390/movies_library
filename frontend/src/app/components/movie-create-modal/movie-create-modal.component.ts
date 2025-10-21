import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Movie } from '../../models/movie.model';
import { MovieService } from '../../services/movie.service';

@Component({
  selector: 'app-movie-create-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './movie-create-modal.component.html',
  styleUrl: './movie-create-modal.component.scss',
})
export class MovieCreateModalComponent implements OnInit {
  @Input() isOpen = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() movieCreated = new EventEmitter<Movie>();

  createForm: FormGroup;
  loading = false;
  error: string | null = null;

  constructor(
    private movieService: MovieService,
    private formBuilder: FormBuilder
  ) {
    this.createForm = this.buildForm();
  }

  ngOnInit(): void {
    if (this.isOpen) {
      this.initializeForm();
    }
  }

  ngOnChanges(): void {
    if (this.isOpen) {
      this.initializeForm();
    }
  }

  buildForm(): FormGroup {
    return this.formBuilder.group({
      name: ['', [Validators.required, Validators.minLength(1)]],
      realisator: ['', [Validators.required, Validators.minLength(1)]],
      rating: [0, [Validators.required, Validators.min(0), Validators.max(10)]],
      duration: [''],
    });
  }

  initializeForm(): void {
    this.createForm.reset({
      name: '',
      realisator: '',
      rating: 0,
      duration: '',
    });
    this.error = null;
  }

  onSave(): void {
    if (this.createForm.invalid) {
      this.markAllFieldsAsTouched();
      return;
    }

    this.loading = true;
    this.error = null;

    const formValue = this.createForm.value;
    const newMovie: Omit<Movie, 'id'> = {
      name: formValue.name,
      realisator: formValue.realisator,
      rating: formValue.rating,
      duration: formValue.duration ? this.formatDurationForApi(formValue.duration) : null,
    };

    this.movieService.createMovie(newMovie).subscribe({
      next: (created) => {
        this.movieCreated.emit(created);
        this.onCancel();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error creating movie:', err);
        this.error = 'Failed to create movie. Please try again.';
        this.loading = false;
      },
    });
  }

  onCancel(): void {
    this.error = null;
    this.createForm.reset();
    this.closeModal.emit();
  }

  markAllFieldsAsTouched(): void {
    Object.keys(this.createForm.controls).forEach(key => {
      this.createForm.get(key)?.markAsTouched();
    });
  }

  getFieldError(fieldName: string): string | null {
    const field = this.createForm.get(fieldName);
    if (field?.errors && field.touched) {
      if (field.errors['required']) {
        return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} is required`;
      }
      if (field.errors['minlength']) {
        return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} is too short`;
      }
      if (field.errors['min']) {
        return 'Rating must be at least 0';
      }
      if (field.errors['max']) {
        return 'Rating must be at most 10';
      }
    }
    return null;
  }

  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }

  formatDurationForApi(timeString: string): string {
    if (!timeString) return '';
    // Convert "02:22" to "02:22:00" for API
    return `${timeString}:00`;
  }
}
