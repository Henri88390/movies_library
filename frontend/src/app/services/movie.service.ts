import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Movie } from '../models/movie.model';

@Injectable({
  providedIn: 'root',
})
export class MovieService {
  private apiUrl = 'http://localhost:5176/api/movies';

  constructor(private http: HttpClient) {}

  getMovies(): Observable<Movie[]> {
    return this.http.get<Movie[]>(`${this.apiUrl}/movies`);
  }

  getMovie(id: number): Observable<Movie> {
    return this.http.get<Movie>(`${this.apiUrl}/${id}`);
  }

  createMovie(movie: Omit<Movie, 'id'>, imageFile?: File): Observable<Movie> {
    const formData = new FormData();
    formData.append('name', movie.name);
    formData.append('realisator', movie.realisator);
    formData.append('rating', movie.rating.toString());

    if (movie.duration) {
      formData.append('duration', movie.duration);
    }

    if (imageFile) {
      formData.append('image', imageFile);
    }

    return this.http.post<Movie>(`${this.apiUrl}/movies`, formData);
  }

  updateMovie(
    id: number,
    movie: Partial<Movie>,
    imageFile?: File,
    removeImage?: boolean
  ): Observable<Movie> {
    const formData = new FormData();

    if (movie.name !== undefined) {
      formData.append('name', movie.name);
    }
    if (movie.realisator !== undefined) {
      formData.append('realisator', movie.realisator);
    }
    if (movie.rating !== undefined) {
      formData.append('rating', movie.rating.toString());
    }
    if (movie.duration !== undefined && movie.duration !== null) {
      formData.append('duration', movie.duration);
    }
    if (imageFile) {
      formData.append('image', imageFile);
    }
    if (removeImage) {
      formData.append('removeImage', 'true');
    }

    return this.http.put<Movie>(`${this.apiUrl}/${id}`, formData);
  }

  deleteMovie(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
