import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  OnInit,
  Output,
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Movie } from '../../models/movie.model';
import { MovieService } from '../../services/movie.service';
import { StarRatingComponent } from '../star-rating/star-rating.component';

@Component({
  selector: 'app-movie-edit-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StarRatingComponent],
  templateUrl: './movie-edit-modal.component.html',
  styleUrl: './movie-edit-modal.component.scss',
})
export class MovieEditModalComponent implements OnInit, OnChanges {
  @Input() movie: Movie | null = null;
  @Input() isOpen = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() movieUpdated = new EventEmitter<Movie>();

  editForm: FormGroup;
  loading = false;
  error: string | null = null;
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  currentImagePath: string | null = null;
  removeCurrentImage = false;

  constructor(
    private movieService: MovieService,
    private formBuilder: FormBuilder
  ) {
    this.editForm = this.createForm();
  }

  ngOnInit(): void {
    if (this.movie) {
      this.initializeForm();
    }
  }

  ngOnChanges(): void {
    if (this.movie) {
      this.initializeForm();
    }
  }

  createForm(): FormGroup {
    return this.formBuilder.group({
      name: ['', [Validators.required, Validators.minLength(1)]],
      realisator: ['', [Validators.required, Validators.minLength(1)]],
      rating: [0, [Validators.required, Validators.min(0), Validators.max(10)]],
      duration: ['', [this.durationValidator]],
    });
  }

  // Custom validator for duration
  durationValidator(control: any) {
    if (!control.value) return null; // Optional field

    const timeValue = control.value;
    const [hours, minutes] = timeValue.split(':').map(Number);
    const totalMinutes = hours * 60 + minutes;
    const maxMinutes = 10 * 60; // 10 hours in minutes

    return totalMinutes > maxMinutes ? { durationTooLong: true } : null;
  }

  initializeForm(): void {
    if (this.movie) {
      this.editForm.patchValue({
        name: this.movie.name,
        realisator: this.movie.realisator,
        rating: this.movie.rating,
        duration: this.formatDurationForInput(this.movie.duration),
      });
      this.currentImagePath = this.movie.imagePath || null;
      this.selectedFile = null;
      this.imagePreview = null;
      this.removeCurrentImage = false;
      this.error = null;
    }
  }

  onSave(): void {
    if (!this.movie || this.editForm.invalid) {
      this.markAllFieldsAsTouched();
      return;
    }

    this.loading = true;
    this.error = null;

    const formValue = this.editForm.value;
    const updatedMovie: Partial<Movie> = {
      name: formValue.name,
      realisator: formValue.realisator,
      rating: formValue.rating,
      duration: formValue.duration
        ? this.formatDurationForApi(formValue.duration)
        : null,
    };

    this.movieService
      .updateMovie(
        this.movie.id,
        updatedMovie,
        this.selectedFile || undefined,
        this.removeCurrentImage
      )
      .subscribe({
        next: (updated) => {
          this.movieUpdated.emit(updated);
          this.onCancel();
          this.loading = false;
        },
        error: (err) => {
          console.error('Error updating movie:', err);
          this.error = 'Failed to update movie. Please try again.';
          this.loading = false;
        },
      });
  }

  onCancel(): void {
    this.error = null;
    this.editForm.reset();
    this.closeModal.emit();
  }

  markAllFieldsAsTouched(): void {
    Object.keys(this.editForm.controls).forEach((key) => {
      this.editForm.get(key)?.markAsTouched();
    });
  }

  getFieldError(fieldName: string): string | null {
    const field = this.editForm.get(fieldName);
    if (field?.errors && field.touched) {
      if (field.errors['required']) {
        return `${
          fieldName.charAt(0).toUpperCase() + fieldName.slice(1)
        } is required`;
      }
      if (field.errors['minlength']) {
        return `${
          fieldName.charAt(0).toUpperCase() + fieldName.slice(1)
        } is too short`;
      }
      if (field.errors['min']) {
        return 'Rating must be at least 0';
      }
      if (field.errors['max']) {
        return 'Rating must be at most 10';
      }
      if (field.errors['durationTooLong']) {
        return 'Duration cannot exceed 10 hours';
      }
    }
    return null;
  }

  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: KeyboardEvent): void {
    if (this.isOpen && !this.loading) {
      this.onCancel();
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];

      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.error = 'Please select a valid image file';
        return;
      }

      // Validate file size (5MB limit)
      if (file.size > 5 * 1024 * 1024) {
        this.error = 'Image file size must be less than 5MB';
        return;
      }

      this.selectedFile = file;
      this.removeCurrentImage = false;
      this.error = null;

      // Create preview
      const reader = new FileReader();
      reader.onload = () => {
        this.imagePreview = reader.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  removeImage(): void {
    this.selectedFile = null;
    this.imagePreview = null;
    this.removeCurrentImage = true;
    // Reset the file input
    const fileInput = document.getElementById('imageFile') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  getCurrentImageUrl(): string {
    if (this.currentImagePath) {
      // If it's already a full URL (starts with http/https), return as-is
      if (
        this.currentImagePath.startsWith('http://') ||
        this.currentImagePath.startsWith('https://')
      ) {
        return this.currentImagePath;
      }
      // Otherwise, it's a local path - prepend the backend URL
      return `http://localhost:5176${this.currentImagePath}`;
    }
    return '';
  }

  hasCurrentImage(): boolean {
    return !!(this.currentImagePath && !this.removeCurrentImage);
  }

  formatDurationForInput(duration: string | null): string {
    if (!duration) return '';
    // Convert "02:22:00" to "02:22" for HTML time input
    const parts = duration.split(':');
    return `${parts[0]}:${parts[1]}`;
  }

  formatDurationForApi(timeString: string): string {
    if (!timeString) return '';
    // Convert "02:22" to "02:22:00" for API
    return `${timeString}:00`;
  }
}
