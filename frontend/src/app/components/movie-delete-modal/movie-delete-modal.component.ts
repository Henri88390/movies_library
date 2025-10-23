import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
} from '@angular/core';
import { Movie } from '../../models/movie.model';
import { MovieService } from '../../services/movie.service';

@Component({
  selector: 'app-movie-delete-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './movie-delete-modal.component.html',
  styleUrl: './movie-delete-modal.component.scss',
})
export class MovieDeleteModalComponent {
  @Input() movie: Movie | null = null;
  @Input() isOpen = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() movieDeleted = new EventEmitter<number>();

  loading = false;
  error: string | null = null;

  constructor(private movieService: MovieService) {}

  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: KeyboardEvent): void {
    if (this.isOpen && !this.loading) {
      this.onCancel();
    }
  }

  onDelete(): void {
    if (!this.movie) return;

    this.loading = true;
    this.error = null;

    this.movieService.deleteMovie(this.movie.id).subscribe({
      next: () => {
        this.movieDeleted.emit(this.movie!.id);
        this.onCancel();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error deleting movie:', err);
        this.error = 'Failed to delete movie. Please try again.';
        this.loading = false;
      },
    });
  }

  onCancel(): void {
    this.error = null;
    this.closeModal.emit();
  }

  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }
}
