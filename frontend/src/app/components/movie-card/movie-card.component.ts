import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Movie } from '../../models/movie.model';

@Component({
  selector: 'app-movie-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './movie-card.component.html',
  styleUrls: ['./movie-card.component.scss'],
})
export class MovieCardComponent {
  @Input({ required: true }) movie!: Movie;
  @Input() isLoggedIn: boolean = false;
  @Output() cardClick = new EventEmitter<Movie>();
  @Output() deleteClick = new EventEmitter<Movie>();

  /**
   * Return an array of class names for the 5 star slots: 'full' | 'half' | 'empty'
   * The backend rating is on a 0-10 scale; we render 5 stars. An odd rating
   * (e.g. 7) becomes 3 full + 1 half (3.5 stars). Even ratings map cleanly.
   */
  getStars(rating: number): string[] {
    const fullStars = Math.floor(rating / 2);
    const hasHalf = rating % 2 === 1; // uneven rating indicates a half
    const emptyStars = 5 - fullStars - (hasHalf ? 1 : 0);

    const stars: string[] = [];
    for (let i = 0; i < fullStars; i++) {
      stars.push('full');
    }
    if (hasHalf) {
      stars.push('half');
    }
    for (let i = 0; i < emptyStars; i++) {
      stars.push('empty');
    }
    return stars;
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

  onDeleteClick(event: Event): void {
    event.stopPropagation(); // Prevent card click when delete button is clicked
    this.deleteClick.emit(this.movie);
  }

  getImageUrl(imagePath: string): string {
    // If it's already a full URL (starts with http/https), return as-is
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
      return imagePath;
    }
    // Otherwise, it's a local path - prepend the backend URL
    return `http://localhost:5176${imagePath}`;
  }
}
