import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Movie } from '../models/movie.model';
import { MovieService } from '../services/movie.service';

@Component({
  selector: 'app-movies',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="movies-container">
      <header class="movies-header">
        <h1>🎬 Movies Library</h1>
        <p>Discover and explore your favorite movies</p>
      </header>

      <div class="loading" *ngIf="loading">
        <div class="spinner"></div>
        <p>Loading movies...</p>
      </div>

      <div class="error" *ngIf="error">
        <p>❌ {{ error }}</p>
        <button (click)="loadMovies()" class="retry-btn">Try Again</button>
      </div>

      <div class="movies-grid" *ngIf="!loading && !error">
        <div class="movie-card" *ngFor="let movie of movies">
          <div class="movie-header">
            <h3>{{ movie.name }}</h3>
            <div class="rating">
              <span class="stars">{{ getStars(movie.rating) }}</span>
              <span class="rating-text">{{ movie.rating }}/10</span>
            </div>
          </div>

          <div class="movie-details">
            <p class="director">
              <strong>Director:</strong> {{ movie.realisator }}
            </p>
            <p class="duration">
              <strong>Duration:</strong> {{ formatDuration(movie.duration) }}
            </p>
          </div>
        </div>
      </div>

      <div
        class="empty-state"
        *ngIf="!loading && !error && movies.length === 0"
      >
        <h2>📽️ No movies found</h2>
        <p>Your movie library is empty. Add some movies to get started!</p>
      </div>
    </div>
  `,
  styles: [
    `
      .movies-container {
        max-width: 1200px;
        margin: 0 auto;
        padding: 20px;
        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      }

      .movies-header {
        text-align: center;
        margin-bottom: 40px;
        padding: 30px 0;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        border-radius: 15px;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
      }

      .movies-header h1 {
        margin: 0 0 10px 0;
        font-size: 2.5rem;
        font-weight: 700;
      }

      .movies-header p {
        margin: 0;
        font-size: 1.1rem;
        opacity: 0.9;
      }

      .loading {
        text-align: center;
        padding: 60px 20px;
      }

      .spinner {
        width: 40px;
        height: 40px;
        border: 4px solid #f3f3f3;
        border-top: 4px solid #667eea;
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin: 0 auto 20px;
      }

      @keyframes spin {
        0% {
          transform: rotate(0deg);
        }
        100% {
          transform: rotate(360deg);
        }
      }

      .error {
        text-align: center;
        padding: 40px 20px;
        color: #e74c3c;
      }

      .retry-btn {
        background: #667eea;
        color: white;
        border: none;
        padding: 10px 20px;
        border-radius: 5px;
        cursor: pointer;
        font-size: 1rem;
        margin-top: 10px;
        transition: background-color 0.3s;
      }

      .retry-btn:hover {
        background: #5a6fd8;
      }

      .movies-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
        gap: 25px;
        margin-top: 30px;
      }

      .movie-card {
        background: white;
        border-radius: 12px;
        padding: 24px;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
        border: 1px solid #e8e8e8;
        transition: all 0.3s ease;
        cursor: pointer;
      }

      .movie-card:hover {
        transform: translateY(-4px);
        box-shadow: 0 8px 30px rgba(0, 0, 0, 0.12);
      }

      .movie-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 16px;
        gap: 15px;
      }

      .movie-header h3 {
        margin: 0;
        font-size: 1.25rem;
        font-weight: 600;
        color: #2c3e50;
        flex-grow: 1;
        line-height: 1.4;
      }

      .rating {
        text-align: right;
        flex-shrink: 0;
      }

      .stars {
        display: block;
        font-size: 1.2rem;
        margin-bottom: 4px;
      }

      .rating-text {
        font-size: 0.9rem;
        color: #7f8c8d;
        font-weight: 500;
      }

      .movie-details p {
        margin: 8px 0;
        color: #555;
        line-height: 1.5;
      }

      .movie-details strong {
        color: #2c3e50;
      }

      .director {
        color: #667eea;
      }

      .duration {
        color: #7f8c8d;
      }

      .empty-state {
        text-align: center;
        padding: 80px 20px;
        color: #7f8c8d;
      }

      .empty-state h2 {
        margin-bottom: 16px;
        color: #2c3e50;
      }

      @media (max-width: 768px) {
        .movies-grid {
          grid-template-columns: 1fr;
          gap: 20px;
        }

        .movies-header h1 {
          font-size: 2rem;
        }

        .movie-header {
          flex-direction: column;
          align-items: flex-start;
          gap: 12px;
        }

        .rating {
          text-align: left;
        }
      }
    `,
  ],
})
export class MoviesComponent implements OnInit {
  movies: Movie[] = [];
  loading = true;
  error: string | null = null;

  constructor(private movieService: MovieService) {}

  ngOnInit(): void {
    this.loadMovies();
  }

  loadMovies(): void {
    this.loading = true;
    this.error = null;

    this.movieService.getMovies().subscribe({
      next: (movies) => {
        this.movies = movies;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading movies:', err);
        this.error =
          'Failed to load movies. Please check if the API is running.';
        this.loading = false;
      },
    });
  }

  getStars(rating: number): string {
    const fullStars = Math.floor(rating / 2);
    const halfStar = rating % 2 >= 1;
    const emptyStars = 5 - fullStars - (halfStar ? 1 : 0);

    return (
      '★'.repeat(fullStars) + (halfStar ? '☆' : '') + '☆'.repeat(emptyStars)
    );
  }

  formatDuration(duration: string): string {
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
}
