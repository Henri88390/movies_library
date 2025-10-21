import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Movie } from '../../models/movie.model';

@Component({
  selector: 'app-movie-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './movie-card.component.html',
  styleUrl: './movie-card.component.scss',
})
export class MovieCardComponent {
  @Input({ required: true }) movie!: Movie;
  @Output() cardClick = new EventEmitter<Movie>();

  getStars(rating: number): string {
    const fullStars = Math.floor(rating / 2);
    const halfStar = rating % 2 >= 1;
    const emptyStars = 5 - fullStars - (halfStar ? 1 : 0);

    return (
      '★'.repeat(fullStars) + (halfStar ? '☆' : '') + '☆'.repeat(emptyStars)
    );
  }

  formatDuration(duration: string | null): string | null {
    // Handle null or undefined duration
    if (!duration) {
      return null;
    }

    // Convert "02:22:00" format to "2h 22m"
    const parts = duration.split(':');
    const hours = parseInt(parts[0], 10);
    const minutes = parseInt(parts[1], 10);

    if (hours === 0) {
      return `${minutes}m`;
    } else if (minutes === 0) {
      return `${hours}h`;
    } else {
      return `${hours}h ${minutes}m`;
    }
  }

  onCardClick(): void {
    this.cardClick.emit(this.movie);
  }
}
