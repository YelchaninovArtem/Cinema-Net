export interface MovieSummary {
  id: number;
  title: string;
  posterUrl: string | null;
  durationMinutes: number;
  ageRating: string;
  genres: string[];
  availableFormats: string[];
}

export interface MovieDetail {
  id: number;
  title: string;
  description: string;
  posterUrl: string | null;
  trailerUrl: string | null;
  durationMinutes: number;
  ageRating: string;
  releaseDateUtc: string;
  genres: string[];
}

export interface Showtime {
  id: number;
  movieId: number;
  movieTitle: string;
  hallName: string;
  cinemaBranchName: string;
  city: string;
  startUtc: string;
  format: string;
  basePrice: number;
}

export interface CinemaBranch {
  id: number;
  name: string;
  city: string;
  address: string;
}

export interface Genre {
  id: number;
  name: string;
}

export interface MovieFilters {
  title?: string;
  city?: string;
  date?: string;
  format?: string;
  genreId?: number;
}
