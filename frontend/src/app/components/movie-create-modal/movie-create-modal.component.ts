import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  Input,
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
  selector: 'app-movie-create-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StarRatingComponent],
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
  selectedFile: File | null = null;
  imagePreview: string | null = null;

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
    this.createForm.reset({
      name: '',
      realisator: '',
      rating: 0,
      duration: '',
    });
    this.error = null;
    this.selectedFile = null;
    this.imagePreview = null;
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
      duration: formValue.duration
        ? this.formatDurationForApi(formValue.duration)
        : null,
    };

    this.movieService
      .createMovie(newMovie, this.selectedFile || undefined)
      .subscribe({
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
    this.selectedFile = null;
    this.imagePreview = null;
    this.closeModal.emit();
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
    // Reset the file input
    const fileInput = document.getElementById('imageFile') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  markAllFieldsAsTouched(): void {
    Object.keys(this.createForm.controls).forEach((key) => {
      this.createForm.get(key)?.markAsTouched();
    });
  }

  getFieldError(fieldName: string): string | null {
    const field = this.createForm.get(fieldName);
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

  formatDurationForApi(timeString: string): string {
    if (!timeString) return '';
    // Convert "02:22" to "02:22:00" for API
    return `${timeString}:00`;
  }
}
