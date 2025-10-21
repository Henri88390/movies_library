import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Movie } from '../../models/movie.model';
import { MovieService } from '../../services/movie.service';
import { MovieCardComponent } from '../movie-card/movie-card.component';
import { MovieCreateModalComponent } from '../movie-create-modal/movie-create-modal.component';
import { MovieEditModalComponent } from '../movie-edit-modal/movie-edit-modal.component';

@Component({
  selector: 'app-movies',
  standalone: true,
  imports: [
    CommonModule,
    MovieCardComponent,
    MovieEditModalComponent,
    MovieCreateModalComponent,
  ],
  templateUrl: './movies.component.html',
  styleUrl: './movies.component.scss',
})
export class MoviesComponent implements OnInit {
  movies: Movie[] = [];
  loading = true;
  error: string | null = null;
  isEditModalOpen = false;
  isCreateModalOpen = false;
  currentMovie: Movie | null = null;

  constructor(private movieService: MovieService) {}

  ngOnInit(): void {
    this.loadMovies();
  }

  handleCloseModal() {
    this.isEditModalOpen = false;
    this.currentMovie = null;
  }

  handleCloseCreateModal() {
    this.isCreateModalOpen = false;
  }

  handleCardClick(movie: Movie) {
    this.isEditModalOpen = true;
    this.currentMovie = movie;
  }

  handleCreateMovie() {
    this.isCreateModalOpen = true;
  }

  handleMovieCreated(newMovie: Movie) {
    this.movies = [...this.movies, newMovie];
  }

  handleMovieUpdated(updatedMovie: Movie) {
    const index = this.movies.findIndex((m) => m.id === updatedMovie.id);
    if (index !== -1) {
      this.movies[index] = updatedMovie;
      this.movies = [...this.movies];
    }
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
}
